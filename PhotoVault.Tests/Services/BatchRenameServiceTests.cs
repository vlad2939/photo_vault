using Dapper;
using PhotoVault.Core.Models;
using PhotoVault.Core.Services;
using PhotoVault.Core.Utils;
using PhotoVault.Data.Repositories;

namespace PhotoVault.Tests.Services;

public sealed class BatchRenameServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PhotoVaultRename", Guid.NewGuid().ToString("N"));
    private readonly BatchRenameService _service;

    public BatchRenameServiceTests()
    {
        Directory.CreateDirectory(_folder);
        _service = new BatchRenameService(new PhotoRepository(_db.Context));
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    private string Touch(string name, DateTime? date = null)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, [1, 2, 3]);
        if (date is not null) File.SetLastWriteTime(path, date.Value);
        return path;
    }

    private static RenamePattern Parse(string pattern) =>
        RenamePattern.TryParse(pattern, out _, out _) ?? throw new InvalidOperationException(pattern);

    private IReadOnlyList<RenamePlanItem> Plan(string pattern, int start = 1, RenameOrder order = RenameOrder.ByName) =>
        _service.BuildPlan(_folder, _service.GetFiles(_folder), Parse(pattern), start, order);

    // ------------------------------------------------------------------ Pattern

    [Theory]
    [InlineData("Vacanta_Grecia_{counter:000}", "IMG_1234.JPG", 7, "Vacanta_Grecia_007")]
    [InlineData("{name}_{counter}", "IMG_1.jpg", 12, "IMG_1_12")]
    [InlineData("{date}_{name}", "a.cr2", 1, "2021-08-10_a")]
    [InlineData("{date:yyyyMMdd}-{counter:00}", "a.jpg", 3, "20210810-03")]
    [InlineData("{NAME}.{ext}", "poza.png", 1, "poza")]          // .{ext} final e implicit
    [InlineData("{name}_{ext}", "poza.png", 1, "poza_png")]
    public void Pattern_FormatsTokens(string pattern, string original, int counter, string expected) =>
        Assert.Equal(expected, Parse(pattern).FormatStem(original, counter, new DateTime(2021, 8, 10, 9, 30, 0)));

    [Theory]
    [InlineData("", RenamePatternError.Empty)]
    [InlineData("   ", RenamePatternError.Empty)]
    [InlineData("poza_{counter", RenamePatternError.UnbalancedBrace)]
    [InlineData("poza}", RenamePatternError.UnbalancedBrace)]
    [InlineData("{foo}", RenamePatternError.UnknownToken)]
    [InlineData("{counter:abc}", RenamePatternError.UnknownToken)]
    [InlineData("{name:x}", RenamePatternError.UnknownToken)]
    public void Pattern_RejectsInvalid(string pattern, RenamePatternError expected)
    {
        Assert.Null(RenamePattern.TryParse(pattern, out var error, out _));
        Assert.Equal(expected, error);
    }

    // ------------------------------------------------------------------ Plan

    [Fact]
    public void GetFiles_OnlySupportedFormats_TopLevel()
    {
        Touch("b.jpg");
        Touch("a.CR2");
        Touch("note.txt");
        Directory.CreateDirectory(Path.Combine(_folder, "sub"));
        File.WriteAllBytes(Path.Combine(_folder, "sub", "c.jpg"), [1]);

        Assert.Equal(["a.CR2", "b.jpg"], _service.GetFiles(_folder).Select(f => f.FileName));
    }

    [Fact]
    public void BuildPlan_KeepsExtension_AndOrdersByDate()
    {
        Touch("z.jpg", new DateTime(2020, 1, 1));
        Touch("a.NEF", new DateTime(2021, 1, 1));

        var byDate = Plan("Poza_{counter:00}", 1, RenameOrder.ByDate);
        Assert.Equal(["z.jpg", "a.NEF"], byDate.Select(p => p.OriginalName));
        Assert.Equal(["Poza_01.jpg", "Poza_02.NEF"], byDate.Select(p => p.NewName));
        Assert.All(byDate, p => Assert.Equal(RenameStatus.Ok, p.Status));

        var byName = Plan("Poza_{counter}", 10);
        Assert.Equal(["Poza_10.NEF", "Poza_11.jpg"], byName.Select(p => p.NewName));
    }

    [Fact]
    public void BuildPlan_DetectsConflicts()
    {
        Touch("a.jpg");
        Touch("b.jpg");

        Assert.All(Plan("Fix"), p => Assert.Equal(RenameStatus.Duplicate, p.Status));
        Assert.All(Plan("bad?name_{counter}"), p => Assert.Equal(RenameStatus.Invalid, p.Status));
        Assert.All(Plan("CON"), p => Assert.Equal(RenameStatus.Invalid, p.Status));
        Assert.All(Plan("{name}"), p => Assert.Equal(RenameStatus.Unchanged, p.Status));

    }

    [Fact]
    public void BuildPlan_ExistingForeignFile_IsConflict()
    {
        Touch("a.png");
        Touch("b.jpg");
        File.WriteAllBytes(Path.Combine(_folder, "Poza_2.jpg.txt"), [1]);   // nume asemănător, dar diferit
        Assert.All(Plan("Poza_{counter}"), p => Assert.Equal(RenameStatus.Ok, p.Status));

        // Intrare din afara lotului cu exact numele țintă (comparație fără majuscule)
        Directory.CreateDirectory(Path.Combine(_folder, "POZA_2.JPG"));
        var plan = Plan("Poza_{counter}");
        Assert.Equal(RenameStatus.Ok, plan.Single(p => p.OriginalName == "a.png").Status);
        Assert.Equal(RenameStatus.Exists, plan.Single(p => p.OriginalName == "b.jpg").Status);
    }

    // ------------------------------------------------------------------ Execuție

    [Fact]
    public void Apply_RenamesOnDisk_HandlesSwap()
    {
        Touch("a.jpg");
        File.WriteAllBytes(Path.Combine(_folder, "b.jpg"), [9]);

        // Ordinea după dată inversată față de nume → a primește numele lui b și invers
        File.SetLastWriteTime(Path.Combine(_folder, "a.jpg"), new DateTime(2022, 1, 1));
        File.SetLastWriteTime(Path.Combine(_folder, "b.jpg"), new DateTime(2020, 1, 1));
        var plan = _service.BuildPlan(_folder, _service.GetFiles(_folder), Parse("{counter}"), 1, RenameOrder.ByDate);
        Assert.All(plan, p => Assert.Equal(RenameStatus.Ok, p.Status));

        var result = _service.Apply(_folder, plan);

        Assert.Equal(2, result.Renamed);
        Assert.Equal([9], File.ReadAllBytes(Path.Combine(_folder, "1.jpg")));   // fostul b.jpg (cel mai vechi)
        Assert.Equal([1, 2, 3], File.ReadAllBytes(Path.Combine(_folder, "2.jpg")));
        Assert.Equal(["1.jpg", "2.jpg"], Directory.GetFiles(_folder).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Apply_SwapNames_AtoB_BtoA()
    {
        File.WriteAllBytes(Path.Combine(_folder, "1.jpg"), [1]);
        File.WriteAllBytes(Path.Combine(_folder, "2.jpg"), [2]);
        File.SetLastWriteTime(Path.Combine(_folder, "1.jpg"), new DateTime(2022, 1, 1));
        File.SetLastWriteTime(Path.Combine(_folder, "2.jpg"), new DateTime(2020, 1, 1));

        var plan = Plan("{counter}", 1, RenameOrder.ByDate);   // 2.jpg → 1.jpg, 1.jpg → 2.jpg
        _service.Apply(_folder, plan);

        Assert.Equal([2], File.ReadAllBytes(Path.Combine(_folder, "1.jpg")));
        Assert.Equal([1], File.ReadAllBytes(Path.Combine(_folder, "2.jpg")));
    }

    [Fact]
    public void Apply_RefusesPlanWithConflicts()
    {
        Touch("a.jpg");
        Touch("b.jpg");
        Assert.Throws<InvalidOperationException>(() => _service.Apply(_folder, Plan("same")));
        Assert.True(File.Exists(Path.Combine(_folder, "a.jpg")));
    }

    [Fact]
    public void Apply_RefusesWhenTargetAppearedAfterPreview()
    {
        Touch("a.jpg");
        var plan = Plan("nou");
        File.WriteAllBytes(Path.Combine(_folder, "nou.jpg"), [7]);   // apărut între previzualizare și aplicare

        Assert.Throws<InvalidOperationException>(() => _service.Apply(_folder, plan));
        Assert.True(File.Exists(Path.Combine(_folder, "a.jpg")));
        Assert.Equal([7], File.ReadAllBytes(Path.Combine(_folder, "nou.jpg")));
    }

    [Fact]
    public void Apply_UpdatesIndex_KeepingIdTagsAndAlbums()
    {
        var a = Touch("a.jpg");
        var b = Touch("b.jpg");
        Directory.CreateDirectory(Path.Combine(_folder, "sub"));
        var nested = Path.Combine(_folder, "sub", "a.jpg");
        File.WriteAllBytes(nested, [1]);

        using (var c = _db.Context.OpenConnection())
        {
            c.Execute("INSERT INTO SourceFolders (FolderPath, DateAdded) VALUES (@_folder, '2026-01-01T00:00:00')", new { _folder });
            foreach (var path in new[] { a, b, nested })
                c.Execute("INSERT INTO Photos (SourceFolderId, FullPath, FileName, Extension, DateAdded, RotationDegrees) VALUES (1, @path, @name, 'jpg', '2026-01-01T00:00:00', 90)",
                    new { path, name = Path.GetFileName(path) });
        }
        var tags = new TagService(new TagRepository(_db.Context));
        var tag = tags.GetOrCreate("mare");
        tags.Assign(tag.Id, [1]);

        Assert.Equal(2, _service.CountIndexed(_folder));   // sub/a.jpg nu e direct în folder

        var result = _service.Apply(_folder, Plan("Grecia_{counter:000}"));

        Assert.Equal(2, result.Renamed);
        Assert.Equal(2, result.IndexUpdated);
        using var check = _db.Context.OpenConnection();
        var row = check.QuerySingle<(string FullPath, string FileName, long Rotation)>(
            "SELECT FullPath, FileName, RotationDegrees FROM Photos WHERE Id = 1");
        Assert.Equal(Path.Combine(_folder, "Grecia_001.jpg"), row.FullPath);
        Assert.Equal("Grecia_001.jpg", row.FileName);
        Assert.Equal(90, row.Rotation);
        Assert.Equal([1L], tags.GetPhotoIds(tag.Id).Order());
        Assert.Equal(nested, check.ExecuteScalar<string>("SELECT FullPath FROM Photos WHERE Id = 3"));
    }
}
