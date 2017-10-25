using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace CodeRefactoring2 {
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CodeRefactoring2CodeRefactoringProvider)), Shared]
    internal class CodeRefactoring2CodeRefactoringProvider : CodeRefactoringProvider {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context) {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a type declaration node.
            var typeDecl = node as TypeDeclarationSyntax;
            if (typeDecl == null) {
                return;
            }

            // For any type declaration node, create a code action to reverse the identifier text.
            //var action = CodeAction.Create("Reverse type name", c => ReverseTypeNameAsync(context.Document, typeDecl, c));
            var action = CodeAction.Create("Replicar Classe", c => ReplicateMethod(context.Document, typeDecl, c));
            var action2 = CodeAction.Create("Copiar Conteúdo", c => TestMethod(context.Document, typeDecl, c));


            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private async Task<Solution> ReplicateMethod(Document document, TypeDeclarationSyntax typeDecl, CancellationToken c) {
            var mainRoot = await document.GetSyntaxRootAsync();
            var solution = document.Project.Solution;
            var nodesToBeReplicated = mainRoot.DescendantNodes().First(n => n.GetType() == typeDecl.GetType());//.DescendantNodes();
            

            var roots = new Dictionary<Document, SyntaxNode>();

            for (int i=0; i< solution.Projects.Count(); i++) {

                var doc = solution.Projects.ElementAt(i).Documents.FirstOrDefault(d =>
                        d.Name.Remove(d.Name.IndexOf(".cs")) ==
                        typeDecl.Identifier.Text);
                if (doc == null) {
                    
                    solution = solution.Projects.ElementAt(i).AddDocument(typeDecl.Identifier.Text + ".cs", mainRoot).Project.Solution;

                } else {
                    var r = await doc.GetSyntaxRootAsync();
                    var targetNode = r.DescendantNodes().First(n => n.GetType() == typeDecl.GetType()); ;
                    r = r.ReplaceNode(targetNode, nodesToBeReplicated);

                    solution = doc.WithSyntaxRoot(r).Project.Solution;

                    //roots.Add(doc, r);
                }
            }

            //foreach(KeyValuePair<Document, SyntaxNode> kvp in roots) {
            //    solution = kvp.Key.WithSyntaxRoot(kvp.Value).Project.Solution;
            //}

            return solution;
        }

        private async Task<Solution> TestMethod(Document document, TypeDeclarationSyntax typeDecl, CancellationToken c) {
            var root = await document.GetSyntaxRootAsync();

            var altroot = await document.Project.Documents.First(d => d.Name == "Class2.cs").GetSyntaxRootAsync(c);

            var type = typeDecl.GetType();

            var targetClass = root.DescendantNodes().First(n => n.GetType() == type);
            var altLastClass = altroot.DescendantNodes().Last(n => n is ClassDeclarationSyntax);

            if (targetClass.ChildNodes().Count() > 0)
                root = root.ReplaceNode(targetClass.DescendantNodes().First(), altLastClass.DescendantNodes().First());
            else
                root = root.InsertNodesAfter(targetClass, altLastClass.DescendantNodes());

            return document.WithSyntaxRoot(root).Project.Solution;
        }

        private async Task<Solution> ReverseTypeNameAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken) {
            // Produce a reversed version of the type declaration's identifier token.
            var identifierToken = typeDecl.Identifier;
            var newName = new string(identifierToken.Text.ToCharArray().Reverse().ToArray());


            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            var semanticModel2 = await document.Project.Documents.First(d => d.Name == "Class2.cs").GetSemanticModelAsync(cancellationToken);
            //var typeSymbol2 = semanticModel2.GetDeclaredSymbol(, cancellationToken);
            //semanticModel2.

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);
            //newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol2, newName.Replace('1', '2'), optionSet, cancellationToken).ConfigureAwait(false);
            var docid = document.Project.Documents.First(d => d.Name == "Class2.cs").Id;
            var text = await newSolution.GetDocument(docid).GetTextAsync(cancellationToken);


            string s = "";
            foreach (var t in text.Lines.Reverse()) {
                s += t.ToString();
            }

            newSolution = newSolution.Projects.First().RemoveDocument(docid).Solution;
            //newSolution.Projects.First().AddDocument("Class2.cs", text);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}