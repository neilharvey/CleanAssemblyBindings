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
var bindingsToRemove = new List<AssemblyBinding>();
var assemblyBindingElement = document.GetElementsByTagName("assemblyBinding")[0];

foreach (XmlElement dependentAssemblyElement in assemblyBindingElement.ChildNodes)
{
    if (dependentAssemblyElement.GetElementsByTagName("assemblyIdentity")[0] is XmlElement assemblyIdentityElement
        && dependentAssemblyElement.GetElementsByTagName("bindingRedirect")[0] is XmlElement bindingRedirectElement)
    {
        var binding = new AssemblyBinding
        {
            Name = assemblyIdentityElement.GetAttribute("name"),
            PublicKeyToken = assemblyIdentityElement.GetAttribute("publicKeyToken"),
            NewVersion = Version.Parse(bindingRedirectElement.GetAttribute("newVersion")),
            XmlElement = dependentAssemblyElement
        };

        if (!bindings.TryAdd(binding.Name, binding))
        {
            var existingBinding = bindings[binding.Name];

            // If the public key token is in upper case then Visual Studio will keep readding bindings for the same library
            if (existingBinding.NewVersion < binding.NewVersion || existingBinding.PublicKeyToken == existingBinding.PublicKeyToken.ToUpperInvariant())
            {
                bindings[binding.Name] = binding;
                bindingsToRemove.Add(existingBinding);
            }
            else
            {
                bindingsToRemove.Add(binding);
            }
        }
    }
}

if (bindingsToRemove.Count == 0)
{
    Console.WriteLine($"No duplicate bindings found.");
}
else
{
    foreach (var binding in bindingsToRemove)
    {
        assemblyBindingElement.RemoveChild(binding.XmlElement);
    }

    document.Save(configPath);
    Console.WriteLine($"Removed {bindingsToRemove.Count} duplicate bindings:");
    foreach (var binding in bindingsToRemove)
    {
        Console.WriteLine($"- {binding.Name} {binding.NewVersion}");
    }
}

class AssemblyBinding
{
    public string Name { get; init; }
    public string PublicKeyToken { get; init; }
    public Version NewVersion { get; init; }
    public XmlElement XmlElement { get; init; }
}