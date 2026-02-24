using System.IO;

namespace Company.Function.Domain.Images;

public sealed record UploadImageCommand(
    Stream Content,
    string ContentType,
    string FileName);