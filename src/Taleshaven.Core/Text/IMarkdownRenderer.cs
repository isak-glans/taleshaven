namespace Taleshaven.Core.Text;

public interface IMarkdownRenderer
{
    /// <summary>Renderar användarens Markdown till HTML som är sanerad och säker att visa.</summary>
    string ToSafeHtml(string? markdown);
}
