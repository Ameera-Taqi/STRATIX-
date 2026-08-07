/** Download a Blob as a file and revoke the object URL. */
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.rel = 'noopener';
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  // Defer revoke so the browser can start the download.
  setTimeout(() => URL.revokeObjectURL(url), 1_000);
}

/** If the API returned JSON error as a blob, parse the message. */
export async function readBlobErrorMessage(blob: Blob): Promise<string | null> {
  if (!blob.type.includes('json') && !blob.type.includes('text')) {
    return null;
  }
  try {
    const text = await blob.text();
    const parsed = JSON.parse(text) as { message?: string };
    return parsed.message ?? text.slice(0, 200);
  } catch {
    return null;
  }
}
