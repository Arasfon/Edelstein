using Edelstein.Security;

using Microsoft.AspNetCore.Mvc;

using System.IO.Pipelines;
using System.Text.Json;

namespace Edelstein.Server.ActionResults;

public class AsyncEncryptedResult<T> : AsyncEncryptedResult
{
    private readonly int _statusCode;
    private readonly T _responseData;

    public AsyncEncryptedResult(int statusCode, T responseData)
    {
        _statusCode = statusCode;
        _responseData = responseData;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        context.HttpContext.Response.StatusCode = _statusCode;

        Pipe resultPipe = new(new PipeOptions(writerScheduler: PipeScheduler.Inline,
            readerScheduler: PipeScheduler.Inline,
            useSynchronizationContext: false));

        await using Stream resultReaderStream = resultPipe.Reader.AsStream();

        Task serializationTask = SerializeJsonToPipe(resultPipe);
        Task encryptionTask = PayloadCryptor.EncryptAsync(resultReaderStream, context.HttpContext.Response.Body);

        await Task.WhenAll(serializationTask, encryptionTask).ConfigureAwait(false);
    }

    private async Task SerializeJsonToPipe(Pipe resultPipe)
    {
        await using Stream resultWriterStream = resultPipe.Writer.AsStream();
        await JsonSerializer.SerializeAsync(resultWriterStream, _responseData, DefaultJsonSerializerOptions).ConfigureAwait(false);
    }
}
