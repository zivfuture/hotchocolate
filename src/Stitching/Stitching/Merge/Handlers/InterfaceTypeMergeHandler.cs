using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class InterfaceTypeMergeHandler
         : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public InterfaceTypeMergeHandler(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            ITypeInfo left = types.FirstOrDefault(t =>
               t.Definition is InterfaceTypeDefinitionNode);

            if (left == null)
            {
                _next.Invoke(context, types);
            }
            else
            {
                var notMerged = new List<ITypeInfo>(types);

                while (notMerged.Count > 0 && left != null)
                {
                    var leftDef = (InterfaceTypeDefinitionNode)left.Definition;
                    var readyToMerge = new List<ITypeInfo>();
                    left.MoveType(notMerged, readyToMerge);
                    var next = new List<ITypeInfo>(notMerged);

                    for (int i = 0; i < notMerged.Count; i++)
                    {
                        if (notMerged[i].Definition is
                            InterfaceTypeDefinitionNode rightDef
                            && CanBeMerged(leftDef, rightDef))
                        {
                            notMerged[i].MoveType(next, readyToMerge);
                        }
                    }

                    MergeType(context, readyToMerge);
                    notMerged = next;

                    left = notMerged.FirstOrDefault(t =>
                        t.Definition is InterfaceTypeDefinitionNode);
                }

                if (notMerged.Count > 0)
                {
                    _next.Invoke(context, notMerged);
                }
            }
        }

        private void MergeType(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            string name = types[0].Definition.Name.Value;

            if (context.ContainsType(name))
            {
                for (int i = 0; i < types.Count; i++)
                {
                    name = types[i].CreateUniqueName();
                    if (!context.ContainsType(name))
                    {
                        break;
                    }
                }
            }

            List<InterfaceTypeDefinitionNode> definitions = types
                .Select(t => t.Definition)
                .Cast<InterfaceTypeDefinitionNode>()
                .ToList();

            InterfaceTypeDefinitionNode definition = definitions[0]
                .AddSource(name, types.Select(t => t.Schema.Name));

            context.AddType(definition);
        }

        private static bool CanBeMerged(
            InterfaceTypeDefinitionNode left,
            InterfaceTypeDefinitionNode right)
        {
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
                && left.Fields.Count == right.Fields.Count)
            {
                Dictionary<string, FieldDefinitionNode> leftFields =
                    left.Fields.ToDictionary(t => t.Name.Value);
                Dictionary<string, FieldDefinitionNode> rightField =
                    right.Fields.ToDictionary(t => t.Name.Value);

                foreach (string name in leftFields.Keys)
                {
                    FieldDefinitionNode leftField = leftFields[name];
                    if (!rightField.TryGetValue(name,
                        out FieldDefinitionNode rightArgument)
                        || !HasSameType(leftField.Type, rightArgument.Type))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool HasSameShape(
            FieldDefinitionNode left,
            FieldDefinitionNode right)
        {
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
                && HasSameType(left.Type, right.Type)
                && left.Arguments.Count == right.Arguments.Count)
            {
                Dictionary<string, InputValueDefinitionNode> leftArgs =
                    left.Arguments.ToDictionary(t => t.Name.Value);
                Dictionary<string, InputValueDefinitionNode> rightArgs =
                    right.Arguments.ToDictionary(t => t.Name.Value);

                foreach (string name in leftArgs.Keys)
                {
                    InputValueDefinitionNode leftArgument = leftArgs[name];
                    if (!rightArgs.TryGetValue(name,
                        out InputValueDefinitionNode rightArgument)
                        || !HasSameType(leftArgument.Type, rightArgument.Type))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool HasSameType(ITypeNode left, ITypeNode right)
        {
            if (left is NonNullTypeNode lnntn
                && right is NonNullTypeNode rnntn)
            {
                return HasSameType(lnntn.Type, rnntn.Type);
            }

            if (left is ListTypeNode lltn
                && right is ListTypeNode rltn)
            {
                return HasSameType(lltn.Type, rltn.Type);
            }

            if (left is NamedTypeNode lntn
                && right is NamedTypeNode rntn)
            {
                return lntn.Name.Value.Equals(
                    rntn.Name.Value,
                    StringComparison.Ordinal);
            }

            throw new NotSupportedException();
        }
    }
}