// Minimal JS interop for triggering a browser file download from Blazor WASM — there is no
// pure-C# way to save a file to the user's disk from inside the sandbox, this is the smallest
// possible bridge (same technique the legacy Blazor Server app used via NbExport/DownloadCSV).
function downloadTextFile(filename, content) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:text/csv;charset=utf-8," + encodeURIComponent(content);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Same technique as downloadTextFile, for binary content (e.g. the zipped trace file from
// Monitoring > Loggers > Trace Viewer) passed over as a base64 string.
function downloadBinaryFile(filename, base64Content, mimeType) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:" + mimeType + ";base64," + base64Content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
