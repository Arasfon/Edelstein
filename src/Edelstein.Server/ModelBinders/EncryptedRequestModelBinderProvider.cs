using Edelstein.Server.Security;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Reflection;

namespace Edelstein.Server.ModelBinders;

public class EncryptedRequestModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (!context.Metadata.ModelType.GetTypeInfo().IsGenericType ||
            context.Metadata.ModelType.GetGenericTypeDefinition() != typeof(EncryptedRequest<>))
            return null;

        Type[] types = context.Metadata.ModelType.GetGenericArguments();
        Type o = typeof(EncryptedRequestModelBinder<>).MakeGenericType(types);

        return (IModelBinder)Activator.CreateInstance(o)!;
    }
}
