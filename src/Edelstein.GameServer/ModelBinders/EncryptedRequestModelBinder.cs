using Edelstein.GameServer.Security;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Edelstein.GameServer.ModelBinders;

public class EncryptedRequestModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (!bindingContext.HttpContext.Request.Body.CanSeek)
            bindingContext.HttpContext.Request.EnableBuffering();

        bindingContext.HttpContext.Request.Body.Position = 0;
        using StreamReader sr = new(bindingContext.HttpContext.Request.Body);
        string requestBody = await sr.ReadToEndAsync();

        bindingContext.Result = ModelBindingResult.Success(new EncryptedRequest(requestBody));
    }
}
