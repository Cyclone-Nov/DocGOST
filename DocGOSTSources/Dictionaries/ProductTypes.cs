using GostDOC.Common;
using GostDOC.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GostDOC.Dictionaries
{
    public enum ProductTypesDoc
    {
        Materials,
        Others,
        Standard
    }

    public class Product
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("note")]
        public string Note { get; set; }
    }

    public class ProductGroupXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlArray("products")]
        [XmlArrayItem(typeof(Product), ElementName = "product")]
        public List<Product> Products { get; set; } = new List<Product>();

        [XmlArray("subgroups")]
        [XmlArrayItem(typeof(ProductGroupXml), ElementName = "subgroup", IsNullable = true)]
        public List<ProductGroupXml> Groups { get; set; } = new List<ProductGroupXml>();
    }

    [XmlRootAttribute("root", IsNullable = false)]
    public class ProductsXml
    {
        [XmlAttribute("docType")]
        public ProductTypesDoc DocType { get; set; }

        [XmlArray("groups")]
        [XmlArrayItem(typeof(ProductGroupXml), ElementName = "group")]
        public List<ProductGroupXml> ProductGroups { get; set; } = new List<ProductGroupXml>();

        [XmlArray("products")]
        [XmlArrayItem(typeof(Product), ElementName = "product")]
        public List<Product> Products { get; set; } = new List<Product>();
    }

    public class ProductGroup
    {
        public string Name { get; set; }
        public IDictionary<string, Product> ProductsList { get; } = new Dictionary<string, Product>();
        public IDictionary<string, ProductGroup> SubGroups { get; set; }

        public ProductGroup(string aName)
        {
            Name = aName;
        }
    }

    public class Products
    {
        public IDictionary<string, Product> ProductsList { get; } = new Dictionary<string, Product>();
        public IDictionary<string, ProductGroup> Groups { get; set; } = new Dictionary<string, ProductGroup>();
    }

    class ProductTypes
    {
        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private string _filePath;

        public ProductTypesDoc DocType { get; private set; }
        public Products Products { get; } = new Products();

        public ProductTypes(ProductTypesDoc aDocType)
        {
            DocType = aDocType;
        }

        public void Load(string aFileName)
        {
            _filePath = Path.Combine(Environment.CurrentDirectory, Constants.Settings, aFileName);
            
            if (!Import(_filePath))
            {
                if (DocType == ProductTypesDoc.Materials)
                {
                    foreach (var line in Utils.ReadCfgFileLines(Constants.MaterialGroupsCfg))
                    {
                        Products.Groups.Add(line, new ProductGroup(line));
                    }
                    Save();
                }
            }
        }

        public bool Import(string aFilePath)
        {
            if (!File.Exists(aFilePath))
            {
                // No file, quiet return
                return false;
            }

            ProductsXml products = null;
            if (XmlSerializeHelper.LoadXmlStructFile(ref products, aFilePath))
            {
                if (products.DocType != DocType)
                {
                    _log.Error($"Ошибка загрузки файла {aFilePath}! Тип файла {products.DocType} не соответствует типу {DocType}!");
                    return false;
                }

                foreach (var product in products.Products)
                {
                    Products.ProductsList[product.Name] = product;
                }
                foreach (var gp in products.ProductGroups)
                {
                    AddOrUpdateGroup(Products.Groups, gp);
                }
                return true;
            }

            _log.Error($"Ошибка загрузки файла {aFilePath}! Формат файла не соответствует ожиданиям.");
            return false;
        }

        public void Save(string aFilePath = null)
        {
            ProductsXml products = new ProductsXml();
            products.DocType = DocType;

            foreach (var product in Products.ProductsList)
            {
                products.Products.Add(product.Value);
            }
            foreach (var kvp in Products.Groups)
            {
                AddGroup(products.ProductGroups, kvp.Value);
            }
            XmlSerializeHelper.SaveXmlStructFile(products, string.IsNullOrEmpty(aFilePath) ? _filePath : aFilePath);
        }

        /// <summary>
        /// добавить или обновить папки материалов
        /// </summary>
        /// <param name="aGroups">a groups.</param>
        /// <param name="aNewGroup">a new group.</param>
        private void AddOrUpdateGroup(IDictionary<string, ProductGroup> aGroups, ProductGroupXml aNewGroup)
        {
            if (!aGroups.ContainsKey(aNewGroup.Name))
            {
                AddGroup(aGroups, aNewGroup);
            }
            else
            {
                UpdateGroup(aGroups, aNewGroup);
            }
        }

        /// <summary>
        /// добавить новую группу с материалами
        /// </summary>
        /// <param name="aGroups">a groups.</param>
        /// <param name="aGroup">a group.</param>
        private void AddGroup(IDictionary<string, ProductGroup> aGroups, ProductGroupXml aGroup)
        {            
            ProductGroup gp = new ProductGroup(aGroup.Name);
            foreach (var product in aGroup.Products)
            {
                gp.ProductsList[product.Name] = product;
            }
            aGroups.Add(aGroup.Name, gp);

            if (aGroup.Groups != null)
            {
                gp.SubGroups = new Dictionary<string, ProductGroup>();
                foreach (var subGroup in aGroup.Groups)
                {
                    AddOrUpdateGroup(gp.SubGroups, subGroup);
                }
            }            
        }

        /// <summary>
        /// обновить группу материалов
        /// </summary>
        /// <param name="aGroups">a groups.</param>
        /// <param name="aGroup">a group.</param>
        private void UpdateGroup(IDictionary<string, ProductGroup> aGroups, ProductGroupXml aGroup)
        {                         
            // добавим новые материалы
            var baseGroup = aGroups[aGroup.Name];
            foreach (var new_mat in aGroup.Products)
            {
                if (!baseGroup.ProductsList.ContainsKey(new_mat.Name))
                {
                    baseGroup.ProductsList[new_mat.Name] = new_mat;
                }
            }

            // добавим новые подгруппы
            var baseSubGroups = aGroups[aGroup.Name].SubGroups;
            if (aGroup.Groups != null)
            {
                foreach (var newSubGroup in aGroup.Groups)
                {
                    AddOrUpdateGroup(baseSubGroups, newSubGroup);
                }
            }            
        }

        private void AddGroup(List<ProductGroupXml> aGroups, ProductGroup aGroup)
        {
            ProductGroupXml gp = new ProductGroupXml() { Name = aGroup.Name };
            gp.Products.AddRange(aGroup.ProductsList.Values);
            aGroups.Add(gp);

            if (aGroup.SubGroups != null)
            {
                foreach (var group in aGroup.SubGroups)
                {
                    AddGroup(gp.Groups, group.Value);
                }
            }
        }

        public bool AddGroup(string aGroup)
        {
            if (!Products.Groups.ContainsKey(aGroup))
            {
                Products.Groups.Add(aGroup, new ProductGroup(aGroup) 
                {
                    SubGroups = new Dictionary<string, ProductGroup>() 
                });
                return true;
            }
            return false;
        }

        public bool RemoveGroup(string aGroup)
        {
            return Products.Groups.Remove(aGroup);
        }

        public bool AddSubGroup(string aGroup, string aSubGroup)
        {
            ProductGroup gp;
            if (Products.Groups.TryGetValue(aGroup, out gp))
            {
                if (!gp.SubGroups.ContainsKey(aSubGroup))
                {
                    gp.SubGroups.Add(aSubGroup, new ProductGroup(aSubGroup));
                    return true;
                }
            }
            return false;
        }

        public bool RemoveSubGroup(string aGroup, string aSubGroup)
        {
            ProductGroup gp;
            if (Products.Groups.TryGetValue(aGroup, out gp))
            {
                return gp.SubGroups.Remove(aSubGroup);
            }
            return false;
        }

        public bool AddProduct(string aGroup, string aSubGroup, Product aProduct)
        {
            if (string.IsNullOrEmpty(aGroup))
            {
                if (!Products.ProductsList.ContainsKey(aProduct.Name))
                {
                    Products.ProductsList.Add(aProduct.Name, aProduct);
                    return true;
                }
            }
            else
            {
                ProductGroup gp;
                if (Products.Groups.TryGetValue(aGroup, out gp))
                {
                    if (!string.IsNullOrEmpty(aSubGroup))
                    {
                        if (!gp.SubGroups.TryGetValue(aSubGroup, out gp))
                        {
                            return false;
                        }
                    }

                    if (!gp.ProductsList.ContainsKey(aProduct.Name))
                    {
                        gp.ProductsList.Add(aProduct.Name, aProduct);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool RemoveProduct(string aGroup, string aSubGroup, string aName)
        {
            if (string.IsNullOrEmpty(aGroup))
            {
                return Products.ProductsList.Remove(aName);
            }
            else
            {
                ProductGroup gp;
                if (Products.Groups.TryGetValue(aGroup, out gp))
                {
                    if (!string.IsNullOrEmpty(aSubGroup))
                    {
                        if (!gp.SubGroups.TryGetValue(aSubGroup, out gp))
                        {
                            return false;
                        }
                    }
                    return gp.ProductsList.Remove(aName);
                }
            }
            return false;
        }

        public Product GetProduct(string aGroup, string aSubGroup, string aName)
        {
            Product product = null;
            if (string.IsNullOrEmpty(aGroup))
            {
                if (Products.ProductsList.TryGetValue(aName, out product))
                {
                    return product;
                }
            }
            else
            {
                ProductGroup gp;
                if (Products.Groups.TryGetValue(aGroup, out gp))
                {
                    if (!string.IsNullOrEmpty(aSubGroup))
                    {
                        if (!gp.SubGroups.TryGetValue(aSubGroup, out gp))
                        {
                            return null;
                        }
                    }

                    if (gp.ProductsList.TryGetValue(aName, out product))
                    {
                        return product;
                    }
                }
            }
            return product;
        }

        public bool EditGroup(string aOldName, string aNewName)
        {
            ProductGroup gp;
            if (Products.Groups.TryGetValue(aOldName, out gp))
            {
                if (!Products.Groups.ContainsKey(aNewName))
                {
                    gp.Name = aNewName;
                    Products.Groups.Remove(aOldName);
                    Products.Groups.Add(aNewName, gp);
                    return true;
                }
            }
            return false;
        }

        public bool EditSubGroup(string aGroup, string aOldName, string aNewName)
        {
            ProductGroup gp;
            if (Products.Groups.TryGetValue(aGroup, out gp))
            {
                if (gp.SubGroups != null)
                {
                    ProductGroup subgroup;
                    if (gp.SubGroups.TryGetValue(aOldName, out subgroup))
                    {
                        if (!gp.SubGroups.ContainsKey(aNewName))
                        {
                            subgroup.Name = aNewName;
                            gp.SubGroups.Remove(aOldName);
                            gp.SubGroups.Add(aNewName, subgroup);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
