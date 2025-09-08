// For examples, see:
// https://thegraybook.vvvv.org/reference/extending/writing-nodes.html#examples
using System.Xml.Linq;
namespace Main;

public static class Utils
{
    /// <summary>
    /// Recursively collects:
    ///  - The absolute XPath of every leaf (non-parent) element within the subtree rooted at <paramref name="element"/>.
    ///  - (Optionally) the absolute XPath of every attribute on every element in that subtree when <paramref name="CollectAttributes"/> is true.
    ///
    /// If the root element itself has no child elements, its XPath is returned.
    /// Attribute XPaths use the form: element-absolute-xpath + "/@attrName"
    /// (e.g. "/people/person[6]/@id").
    /// </summary>
    /// <param name="element">
    /// The root <see cref="XElement"/> whose descendant leaf element (and optionally attribute) XPaths will be gathered.
    /// </param>
    /// <param name="values">
    /// (Ignored on input) A builder instance used internally to accumulate XPath strings.
    /// A new <see cref="SpreadBuilder{String}"/> is always created at the start of the call.
    /// </param>
    /// <param name="CollectAttributes">
    /// When true, attribute XPaths are included; when false, only leaf element XPaths are returned.
    /// </param>
    /// <returns>
    /// A <see cref="Spread{String}"/> containing:
    ///  - The absolute XPath of each leaf element.
    ///  - (If enabled) The absolute XPath of every attribute encountered.
    /// Order is depth‑first; attributes (when included) are listed before descending to children (and
    /// before the element itself if it is a leaf).
    /// </returns>
    public static Spread<string> GetAllXpaths(XElement element, SpreadBuilder<string> values, bool CollectAttributes)
    {
        values = new SpreadBuilder<string>(); // Initialize the out parameter

        void Traverse(XElement el, SpreadBuilder<string> builder)
        {
            if (CollectAttributes)
            {
                foreach (var attr in el.Attributes())
                {
                    builder.Add(attr.GetAbsoluteXPath());
                }
            }

            if (!el.HasElements)
            {
                builder.Add(el.GetAbsoluteXPath());
            }
            else
            {
                foreach (XElement child in el.Elements())
                {
                    Traverse(child, builder);
                }
            }
        }

        Traverse(element, values);
        return values.ToSpread();
    }

    /// <summary>
    /// Get the absolute XPath to a given XElement
    /// (e.g. "/people/person[6]/name[1]/last[1]").
    /// </summary>
    public static string GetAbsoluteXPath(this XElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        Func<XElement, string> relativeXPath = e =>
        {
            int index = e.IndexPosition();
            string name = e.Name.LocalName;

            return (index == -1)
                ? "/" + name
                : string.Format("/{0}[{1}]", name, index.ToString());
        };

        var ancestors = from e in element.Ancestors()
                        select relativeXPath(e);

        return string.Concat(ancestors.Reverse().ToArray()) + relativeXPath(element);
    }

    /// <summary>
    /// Get the absolute XPath of an attribute (e.g. "/people/person[6]/@id").
    /// </summary>
    public static string GetAbsoluteXPath(this XAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var parent = attribute.Parent
            ?? throw new InvalidOperationException("Detached attribute has no parent element.");

        return parent.GetAbsoluteXPath() + "/@" + attribute.Name.LocalName;
    }

    /// <summary>
    /// Get the index of the given XElement relative to its siblings with identical names.
    /// If the given element is the root, -1 is returned.
    /// </summary>
    public static int IndexPosition(this XElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (element.Parent == null)
        {
            return -1;
        }

        int i = 1;
        foreach (var sibling in element.Parent.Elements(element.Name))
        {
            if (sibling == element)
            {
                return i;
            }
            i++;
        }

        throw new InvalidOperationException("element has been removed from its parent.");
    }
}