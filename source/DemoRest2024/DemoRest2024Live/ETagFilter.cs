
using System.Text;
using System.Text.Json;
using DemoRest2024Live.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace DemoRest2024Live;

public class ETagFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        // Only process GET requests
        if (context.HttpContext.Request.Method != "GET")
        {
            return result;
        }

        // If the result is not successful (not 200), return as is without ETag
        if (result is IStatusCodeHttpResult statusCodeResult && statusCodeResult.StatusCode != 200)
        {
            return result;
        }

        // Ensure the result has a value to serialize
        if (result is IValueHttpResult valueResult && valueResult.Value is not null)
        {
            var content = JsonSerializer.Serialize(valueResult.Value);

            var eTag = ETagGenerator.GetETag(context.HttpContext.Request.Path.ToString(), Encoding.UTF8.GetBytes(content));

            // Check the If-None-Match header for a matching ETag
            if (context.HttpContext.Request.Headers.IfNoneMatch == eTag)
            {
                context.HttpContext.Response.StatusCode = 304;
                return new StatusCodeResult(304); // Not Modified
            }

            // Set the ETag header
            context.HttpContext.Response.Headers.Append("ETag", new[] { eTag });
        }

        return result;
    }
}


//using System.Text;
//using System.Text.Json;
//using DemoRest2024Live.Helpers;
//using Microsoft.AspNetCore.Mvc;

//namespace DemoRest2024Live;

//public class ETagFilter : IEndpointFilter
//{
//    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
//    {
//        var result = await next(context);

//        if (context.HttpContext.Request.Method != "GET") return result;

//        if (context.HttpContext.Response.StatusCode != 200) return result;

//        var content = JsonSerializer.Serialize(((IValueHttpResult)result).Value);

//        var eTag = ETagGenerator.GetETag(context.HttpContext.Request.Path.ToString(), Encoding.UTF8.GetBytes(content));

//        if (context.HttpContext.Request.Headers.IfNoneMatch == eTag)
//        {
//            context.HttpContext.Response.StatusCode = 304;
//            return new StatusCodeResult(304);
//        }

//        context.HttpContext.Response.Headers.Append("ETag", new[] { eTag });
//        return result;
//    }
//}

