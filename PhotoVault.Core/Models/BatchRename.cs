namespace PhotoVault.Core.Models;

/// <summary>Un fișier foto din folderul ales pentru redenumire (§6.8).</summary>
public sealed record RenameSource(string FullPath, DateTime FileDate)
{
    public string FileName => Path.GetFileName(FullPath);
}

/// <summary>Ordinea în care numărătorul <c>{counter}</c> parcurge fișierele.</summary>
public enum RenameOrder
{
    ByName,
    ByDate
}

public enum RenameStatus
{
    /// <summary>Va fi redenumit.</summary>
    Ok,

    /// <summary>Numele rezultat e identic cu cel actual — fișierul e sărit.</summary>
    Unchanged,

    /// <summary>Numele rezultat nu e valid în Windows (caractere interzise, gol, nume rezervat, prea lung).</summary>
    Invalid,

    /// <summary>Două sau mai multe fișiere ar primi același nume.</summary>
    Duplicate,

    /// <summary>În folder există deja un alt fișier (care nu face parte din lot) cu acest nume.</summary>
    Exists
}

/// <summary>Rândul din previzualizare: numele actual → numele rezultat.</summary>
public sealed record RenamePlanItem(RenameSource Source, string NewName, RenameStatus Status)
{
    public string OriginalName => Source.FileName;
    public bool IsConflict => Status is RenameStatus.Invalid or RenameStatus.Duplicate or RenameStatus.Exists;
}

public sealed record RenameResult(int Renamed, int IndexUpdated);
