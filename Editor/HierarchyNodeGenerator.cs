using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HierarchyNode
{
    class NodeInfo
    {
        public string OriginalName { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public List<NodeInfo> ChildNodes { get; set; }
        public List<string> Components { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var y = obj as NodeInfo;
            if (y == null)
            {
                return false;
            }

            return this.Name.Equals(y.Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }

    public class HierarchyNodeGenerator
    {
        public string FileName { get; private set; }

        public string ScriptString { get; private set; }
        public string ComponentScriptString { get; private set; }

        private NodeInfo rootNode;

        public HierarchyNodeGenerator(GameObject go)
        {
            this.rootNode = GetNodeInfo(go.transform);
            this.FileName = string.Format("{0}Node", this.rootNode.Name);

            this.ScriptString = CreateScript(this.rootNode);
            this.ComponentScriptString = CreateComponentScript(this.rootNode);
        }

        private NodeInfo GetNodeInfo(Transform transform, string parentClassName = null)
        {
            var name = FixName(transform.name);
            var className = name;
            if (name == parentClassName)
            {
                className = string.Format("{0}Child", name);
            }

            return new NodeInfo()
            {
                OriginalName = transform.name,
                Name = name,
                ClassName = className,
                ChildNodes = GetChildren(transform, className),
                Components = GetComponents(transform),
            };
        }

        private List<NodeInfo> GetChildren(Transform transform, string parentClassName)
        {
            return transform.Cast<Transform>()
                .Select<Transform, NodeInfo>(t => GetNodeInfo(t, parentClassName))
                .Distinct()
                .ToList();
        }

        private List<string> GetComponents(Transform transform)
        {
            return transform.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType())
                .Select(type => type.ToString())
                .Select(type =>
                {
                    if (type.StartsWith(@"UnityEngine."))
                    {
                        if (type.Count(c => c == '.') == 1)
                        {
                            return Regex.Replace(type, @"^UnityEngine\.", "");
                        }
                    }

                    return type;
                })
                .Where(type => type != "Transform")
                .Distinct()
                .OrderBy(type => type)
                .ToList();
        }

        private string FixName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "_";
            }
            name = ToCamel(Regex.Replace(name, @"[^\w]", " "));
            name = Regex.Replace(name, @"\s", "");
            name = ToCamel(Regex.Replace(name, @"_", " "));
            name = Regex.Replace(name, @"\s", "_");
            if (Regex.IsMatch(name, @"^[0-9]"))
            {
                name = string.Format("_{0}", name);
            }
            return name;
        }

        private string ToCamel(string str)
        {
            return string.Join(" ", str.Split(' ').Where(s => s != "").Select(s => ToHeadUpper(s)).ToArray());
        }

        private string ToHeadUpper(string str)
        {
            if (str.Length == 1)
            {
                return str.ToUpper();
            }
            return string.Join("", new string[] { str.Substring(0, 1).ToUpper(), str.Substring(1) });
        }

        private string AddIndent(string str)
        {
            var indent = new string(' ', 4);
            var lines = str.Split('\n').Select(line =>
            {
                if (string.IsNullOrEmpty(line) || line == "\r")
                {
                    return line;
                }

                return string.Format("{0}{1}", indent, line);
            })
            .ToArray();
            return string.Join("\n", lines);
        }

        private string CreateComponentScript(NodeInfo node)
        {
            return string.Format(COMPONENT_SCRIPT_TEMPLATE, this.FileName, node.ClassName);
        }

        private string CreateScript(NodeInfo node)
        {
            return string.Format(SCRIPT_TEMPLATE, AddIndent(CreateNode(node)));
        }

        private string CreateNode(NodeInfo node)
        {
            var nodeProp = "";

            if (node.Components.Count > 0)
            {
                nodeProp = string.Format("\n\n{0}", AddIndent(CreateNodeProperty(node)));
            }

            if (node.ChildNodes.Count != 0)
            {
                var childClasses = node.ChildNodes.Select(c => CreateNode(c)).ToArray();
                return string.Format(NODE_TEMPLATE, node.ClassName, AddIndent(string.Join("\n\n", childClasses)), nodeProp, CreateNodeChild(node));
            }

            return string.Format(EMPTY_CHILDREN_NODE_TEMPLATE, node.ClassName, nodeProp);
        }

        private string CreateNodeProperty(NodeInfo node)
        {
            var props = node.Components.Select(type => string.Format(NODE_PROPERTY_TEMPLATE, type, type.Split('.').Last())).ToArray();
            return string.Join("\n\n", props);
        }

        private string CreateNodeChild(NodeInfo node)
        {
            var props = node.ChildNodes.Select(c => string.Format(NODE_CHILD_PROPERTY_TEMPLATE, node.ClassName, c.ClassName, c.Name, Regex.Replace(c.OriginalName, "\"", "\\\""))).ToArray();
            return string.Format(NODE_CHILD_TEMPLATE, node.ClassName, AddIndent(string.Join("\n\n", props)));
        }


        private static readonly string COMPONENT_SCRIPT_TEMPLATE = @"using UnityEngine;
using HierarchyNode.Base;

namespace HierarchyNode.Generated
{{
    public class {0}Component : MonoBehaviour
    {{
        private {0} _{1};
        public {0} @{1}
        {{
            get
            {{
                return _{1} ?? (_{1} = new {0}(transform));
            }}
        }}
    }}
}}
";

        private static readonly string SCRIPT_TEMPLATE = @"using UnityEngine;
using HierarchyNode.Base;

namespace HierarchyNode.Generated
{{
{0}
}}
";

        private static readonly string EMPTY_CHILDREN_NODE_TEMPLATE = @"public class {0}Node : EmptyChildrenNodeBase
{{
    public {0}Node(GameObject gameObject) : base(gameObject.transform)
    {{
    }}

    public {0}Node(Transform transform) : base(transform)
    {{
    }}

    public {0}Node(MonoBehaviour mono) : base(mono.transform)
    {{
    }}{1}
}}";

        private static readonly string NODE_TEMPLATE = @"public class {0}Node : NodeBase<{0}NodeChildren>
{{
    public {0}Node(GameObject gameObject) : base(gameObject.transform)
    {{
    }}

    public {0}Node(Transform transform) : base(transform)
    {{
    }}

    public {0}Node(MonoBehaviour mono) : base(mono.transform)
    {{
    }}

{1}{2}
}}

{3}";

        private static readonly string NODE_PROPERTY_TEMPLATE = @"private {0} _{1};
public {0} {1}
{{
    get
    {{
        return _{1} ?? (_{1} = GetComponent<{0}>());
    }}
}}";

        private static readonly string NODE_CHILD_TEMPLATE = @"public class {0}NodeChildren : NodeChildrenBase
{{
{1}
}}";

        private static readonly string NODE_CHILD_PROPERTY_TEMPLATE = @"private {0}Node.{1}Node _{2};
public {0}Node.{1}Node @{2}
{{
    get
    {{
        return _{2} ?? (_{2} = new {0}Node.{1}Node(transform.Find(""{3}"")));
    }}
}}";
    }
}

