using Edelstein.Server.Authorization;
using Edelstein.Server.Security;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Edelstein.Server.ModelBinders;

public class EncryptedRequestModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext.HttpContext.Items.TryGetValue(RsaSignatureAuthorizationFilter.ComputedEncryptedRequestItemName,
                out object? encryptedRequestObject) && encryptedRequestObject is EncryptedRequest encryptedRequest)
        {
            bindingContext.HttpContext.Items.Remove(RsaSignatureAuthorizationFilter.ComputedEncryptedRequestItemName);

            bindingContext.Result = ModelBindingResult.Success(encryptedRequest);

            return;
        }

        using StreamReader sr = new(bindingContext.HttpContext.Request.Body);
        string requestBody = await sr.ReadToEndAsync();

        bindingContext.Result = ModelBindingResult.Success(new EncryptedRequest(requestBody));
    }
}
