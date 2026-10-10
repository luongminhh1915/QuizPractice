namespace QuizPractice.Application.Common;

/// <summary>A file the API sends for download, e.g. the question import template.</summary>
public sealed record FileDownload(byte[] Content, string ContentType, string FileName);
