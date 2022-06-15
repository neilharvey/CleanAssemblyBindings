using System.Xml;

if (args.Length == 0)
{
    Console.WriteLine("Usage: CleanAssemblyBindings [path-to-config]");
    return;
}

var configPath = args[0];
if (!File.Exists(configPath))
{
    Console.WriteLine($"Error: {configPath} does not exist.");
    return;
}

var document = new XmlDocument();
document.Load(configPath);

var bindings = new Dictionary<string, AssemblyBinding>();
var nodesToRemove = new List<XmlElement>();
var assemblyBindingElement = document.GetElementsByTagName("assemblyBinding")[0];

foreach (XmlElement dependentAssemblyElement in assemblyBindingElement.ChildNodes)
{
    if (dependentAssemblyElement.GetElementsByTagName("assemblyIdentity")[0] is XmlElement assemblyIdentityElement
        && dependentAssemblyElement.GetElementsByTagName("bindingRedirect")[0] is XmlElement bindingRedirectElement)
    {
        var binding = new AssemblyBinding
        {
            Name = assemblyIdentityElement.GetAttribute("name"),
            PublicKeyToken = assemblyIdentityElement.GetAttribute("publicKeyToken").ToLower(),
            NewVersion = Version.Parse(bindingRedirectElement.GetAttribute("newVersion")),
            XmlElement = dependentAssemblyElement
        };

        if (!bindings.TryAdd(binding.Name, binding))
        {
            var existingBinding = bindings[binding.Name];
            if (existingBinding.NewVersion < binding.NewVersion)
            {
                bindings[binding.Name] = binding;
                nodesToRemove.Add(existingBinding.XmlElement);
            }
            else
            {
                nodesToRemove.Add(binding.XmlElement);
            }
        }
    }
}

foreach (var element in nodesToRemove)
{
    assemblyBindingElement.RemoveChild(element);
}

document.Save(configPath);
Console.WriteLine($"Removed {nodesToRemove.Count} duplicate bindings");

class AssemblyBinding
{
    public string Name { get; init; }
    public string PublicKeyToken { get; init; }
    public Version NewVersion { get; init; }
    public XmlElement XmlElement { get; init; }
}