using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SAAC.PipelineServices;

namespace ServerApplication.Helpers
{
    /// <summary>
    /// Classe utilitaire pour charger les configurations depuis une DLL et un fichier JSON
    /// </summary>
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Modèle JSON pour les topics
        /// </summary>
        public class TopicConfigEntry
        {
            public string Topic { get; set; }
            public string Type { get; set; } // Nom complet du type (ex: "System.String" ou "MonNamespace.MaClasse")
            public string Class { get; set; } // Format: "Namespace.ClassName, AssemblyName"
            public string? Transformer { get; set; } // Format: "Namespace.TransformerClass, AssemblyName" (optionnel)
        }

        /// <summary>
        /// Charge une DLL et retourne les types disponibles
        /// </summary>
        public static List<Type> LoadAssemblyTypes(string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"La DLL n'a pas été trouvée : {dllPath}");

            try
            {
                Assembly assembly = Assembly.LoadFrom(dllPath);
                return assembly.GetTypes().ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors du chargement de la DLL : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Charge une configuration à partir d'un fichier JSON et l'applique à la configuration Rendezvous
        /// </summary>
        public static void LoadConfigurationFromJson(string jsonPath, RendezVousPipelineConfiguration config, Assembly customAssembly = null)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Le fichier JSON n'a pas été trouvé : {jsonPath}");

            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                List<TopicConfigEntry> entries = JsonConvert.DeserializeObject<List<TopicConfigEntry>>(jsonContent);

                if (entries == null || entries.Count == 0)
                    throw new InvalidOperationException("Le fichier JSON est vide ou invalide");

                foreach (var entry in entries)
                {
                    try
                    {
                        Type dataType = ResolveType(entry.Class, customAssembly);
                        config.TopicsTypes[entry.Topic] = dataType;

                        // Charger le transformer si spécifié
                        if (!string.IsNullOrEmpty(entry.Transformer))
                        {
                            Type transformerType = ResolveType(entry.Transformer, customAssembly);
                            config.Transformers[entry.Topic] = transformerType;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Erreur lors du traitement du topic '{entry.Topic}' : {ex.Message}", ex);
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Erreur de parsing JSON : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Résout un type à partir de son nom qualifié (ex: "System.String, mscorlib")
        /// Cherche dans :
        /// 1. Les types System (Type.GetType)
        /// 2. La DLL personnalisée fournie
        /// 3. Toutes les assemblies chargées dans le domaine (projet + dépendances)
        /// </summary>
        private static Type ResolveType(string typeFullName, Assembly customAssembly = null)
        {
            // Étape 1 : Essayer de charger depuis les assemblies système chargés
            Type type = Type.GetType(typeFullName);
            if (type != null)
                return type;

            // Étape 2 : Si une assembly personnalisée est fournie, chercher dedans
            if (customAssembly != null)
            {
                string typeNameOnly = typeFullName.Split(',')[0].Trim();
                type = customAssembly.GetType(typeNameOnly);
                if (type != null)
                    return type;
            }

            // Étape 3 : Chercher dans toutes les assemblies chargées du domaine
            // (inclut PsiFormats, PipelineServices, et autres composants du projet)
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in loadedAssemblies)
            {
                try
                {
                    string typeNameOnly = typeFullName.Split(',')[0].Trim();
                    type = assembly.GetType(typeNameOnly, false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Ignorer les erreurs d'une assembly particulière et continuer
                    continue;
                }
            }

            // Étape 4 : Si le type inclut un nom d'assembly, essayer de le charger
            if (typeFullName.Contains(","))
            {
                try
                {
                    string[] parts = typeFullName.Split(',');
                    string assemblyName = parts[1].Trim();
                    
                    // Chercher d'abord si elle est déjà chargée
                    foreach (Assembly assembly in loadedAssemblies)
                    {
                        if (assembly.GetName().Name == assemblyName)
                        {
                            type = assembly.GetType(parts[0].Trim(), false);
                            if (type != null)
                                return type;
                        }
                    }

                    // Essayer de charger l'assembly si elle existe
                    Assembly targetAssembly = Assembly.Load(assemblyName);
                    type = targetAssembly.GetType(parts[0].Trim(), false);
                    if (type != null)
                        return type;
                }
                catch
                {
                    // Ignorer les erreurs de chargement et laisser l'exception finale
                }
            }

            throw new TypeLoadException($"Type introuvable : {typeFullName}\n\nAssemblies chargées :\n" + 
                string.Join("\n", loadedAssemblies.Select(a => $"  • {a.GetName().Name}")));
        }

        /// <summary>
        /// Exporte une configuration en JSON pour servir de template
        /// </summary>
        public static void ExportConfigurationTemplate(string outputPath, List<Type> availableTypes)
        {
            List<TopicConfigEntry> entries = new List<TopicConfigEntry>();

            foreach (var type in availableTypes)
            {
                entries.Add(new TopicConfigEntry
                {
                    Topic = type.Name.ToLower(),
                    Type = type.Name,
                    Class = $"{type.FullName}, {type.Assembly.GetName().Name}",
                    Transformer = null
                });
            }

            string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            File.WriteAllText(outputPath, json);
        }

        /// <summary>
        /// Retourne tous les types disponibles du projet
        /// Inclut : types System, types du projet, types PsiFormats, types des DLLs chargées
        /// </summary>
        public static List<Type> GetAvailableProjectTypes()
        {
            List<Type> availableTypes = new List<Type>();
            HashSet<string> typeNames = new HashSet<string>(); // Pour éviter les doublons

            try
            {
                // Charger tous les types des assemblies du domaine
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (Assembly assembly in loadedAssemblies)
                {
                    try
                    {
                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            // Ignorer les types génériques, les types internes, et les interfaces
                            if (!type.IsGenericTypeDefinition && 
                                !type.Name.StartsWith("<") && 
                                type.IsPublic)
                            {
                                string fullName = $"{type.FullName}, {assembly.GetName().Name}";
                                if (!typeNames.Contains(fullName))
                                {
                                    availableTypes.Add(type);
                                    typeNames.Add(fullName);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignorer les erreurs d'une assembly particulière et continuer
                        continue;
                    }
                }

                return availableTypes.OrderBy(t => t.FullName).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la découverte des types disponibles : {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporte tous les types disponibles du projet en JSON (pour exploration)
        /// </summary>
        public static void ExportAvailableTypesAsJson(string outputPath)
        {
            List<Type> availableTypes = GetAvailableProjectTypes();
            ExportConfigurationTemplate(outputPath, availableTypes);
        }
    }
}
