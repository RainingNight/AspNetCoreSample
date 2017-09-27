using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace OAuthSample
{
    public static class HttpResponseExtensions
    {
        public static async Task WriteHtmlAsync(this HttpResponse response, Func<HttpResponse, Task> writeContent)
        {
            var bootstrap = "<link rel=\"stylesheet\" href=\"https://cdn.bootcss.com/bootstrap/3.3.7/css/bootstrap.min.css\" integrity=\"sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u\" crossorigin=\"anonymous\">";

            response.ContentType = "text/html";
            await response.WriteAsync($"<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"UTF-8\">{bootstrap}</head><body><div class=\"container\">");
            await writeContent(response);
            await response.WriteAsync("</div></body></html>");
        }

        public static async Task WriteTableHeader(this HttpResponse response, IEnumerable<string> columns, IEnumerable<IEnumerable<string>> data)
        {
            await response.WriteAsync("<table class=\"table table-condensed\">");
            await response.WriteAsync("<tr>");
            foreach (var column in columns)
            {
                await response.WriteAsync($"<th>{HtmlEncode(column)}</th>");
            }
            await response.WriteAsync("</tr>");
            foreach (var row in data)
            {
                await response.WriteAsync("<tr>");
                foreach (var column in row)
                {
                    await response.WriteAsync($"<td>{HtmlEncode(column)}</td>");
                }
                await response.WriteAsync("</tr>");
            }
            await response.WriteAsync("</table>");
        }

        public static string HtmlEncode(string content) =>
            string.IsNullOrEmpty(content) ? string.Empty : HtmlEncoder.Default.Encode(content);
    }
}
