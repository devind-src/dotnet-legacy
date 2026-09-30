namespace SyncNetPro.Sdk.Core;

/// <summary>Kanal ke Core tidak tersedia (node tidak ada, tidak punya kanal inbound, atau terputus).</summary>
public sealed class CoreUnavailableException : Exception
{
    /// <summary>Membuat exception.</summary>
    public CoreUnavailableException()
    {
    }

    /// <summary>Membuat exception dengan pesan.</summary>
    public CoreUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Membuat exception dengan pesan dan penyebab.</summary>
    public CoreUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Request dengan kunci korelasi yang sama masih menunggu respons Core.</summary>
public sealed class DuplicateCoreRequestException : Exception
{
    /// <summary>Membuat exception.</summary>
    public DuplicateCoreRequestException()
    {
    }

    /// <summary>Membuat exception dengan pesan.</summary>
    public DuplicateCoreRequestException(string message)
        : base(message)
    {
    }

    /// <summary>Membuat exception dengan pesan dan penyebab.</summary>
    public DuplicateCoreRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
