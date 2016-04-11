using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DT_Algorithm
{
    public enum AttributeType
    {
        Discrete,
        Continuous
    }
    public interface IItemAttribute : IComparable
    {
        string Name { get; set; }
        AttributeType AttributeType { get; }
        object RowValue { get; set; }
    }

    public class MyAttribute : IItemAttribute
    {
        public MyAttribute(string name, IComparable value)
        {
            this.Name = name;
            this.RowValue = value;
        }

        public string Name { get; set; }
        public object RowValue { get; set; }

        public AttributeType AttributeType { get; set; } = AttributeType.Discrete;

        public int CompareTo(object obj)
        {
            if (RowValue.GetType() != obj.GetType())
                throw new ArgumentException("Types are not equal");

            return ((IComparable)this.RowValue).CompareTo(obj);
        }
    }

    public class Item
    {
        public Dictionary<string, IItemAttribute> Attributes { get; set; } = new Dictionary<string, IItemAttribute>();
    }
    public class DataSet
    {
        public List<Item> Items { get; set; } = new List<Item>();
    }


    public class Classification
    {
        //class - percent
        public List<Tuple<object, double>> Result = new List<Tuple<object, double>>();
    }
    public class Node
    {
        public string ClassificationAttributeName { get; set; }
        //public Dictionary<string, IItemAttribute> AllowedAttributes { get; set; } = new Dictionary<string, IItemAttribute>();

        public Tree Tree { get; }
        public Node(Tree tree, int height)
        {
            this.Tree = tree;
            this.Height = height;
        }

        public Classification Category { get; set; } = new Classification();
        public string SplitAttribute { get; set; }
        public object SplitValue { get; set; }

        public DataSet Data { get; set; }
        public List<Node> Nodes { get; private set; } = new List<Node>();


        public int Height { get; }
        public void Build()
        {
            double initEntropy = Utility.Entropy(Data, Tree.ClassificationAttributeName);

            //if set consist items with one category
            if (initEntropy == 0)
            {
                Category.Result.Clear();
                Category.Result.Add(new Tuple<object, double>(Data.Items.FirstOrDefault().Attributes[ClassificationAttributeName].RowValue, 100));

                return;
            }

            DataSet bestSplitSkippedSet = null;
            DataSet bestSplitNonSkippedSet = null;

            List<DataSet> bestSplit = null;
            string bestSplitAttribute = null;
            double bestSplitGain = 0;

            #region Split and count best gain
            foreach (var attr in Tree._attributeMap)
            {
                DataSet skippedSet = new DataSet();
                DataSet nonSkippedSet = new DataSet();

                string attrName = attr.Key;
                AttributeType attrType = attr.Value.Item1;

                if (attrName == this.ClassificationAttributeName) continue;

                #region Continuous Attribute
                if (attrType == AttributeType.Continuous)
                {
                    var set = new SortedSet<object>();

                    foreach (var it in Data.Items)
                    {
                        IItemAttribute at = null;
                        if (it.Attributes.TryGetValue(attrName, out at))
                        {
                            var val = at.RowValue;
                            if (val != null)
                            {
                                nonSkippedSet.Items.Add(it);
                                set.Add(val);
                            }
                            else
                                skippedSet.Items.Add(it);
                        }
                        else
                            skippedSet.Items.Add(it);
                    }


                    foreach (var threshold in set)
                    {
                        var split = Utility.SplitContinuous(nonSkippedSet, attrName, threshold);
                        double gain = initEntropy - Utility.Entropy(split, ClassificationAttributeName);

                        if (gain > bestSplitGain)
                        {
                            bestSplitGain = gain;
                            bestSplitAttribute = attrName;
                            bestSplit = split;

                            bestSplitSkippedSet = skippedSet;
                            bestSplitNonSkippedSet = nonSkippedSet;
                        }
                    }
                }
                #endregion
                #region Discrete Attribute
                if (attrType == AttributeType.Discrete)
                {
                    foreach (var it in Data.Items)
                    {
                        IItemAttribute at = null;
                        if (it.Attributes.TryGetValue(attrName, out at))
                        {
                            var val = at.RowValue;
                            if (val != null)
                                nonSkippedSet.Items.Add(it);
                            else
                                skippedSet.Items.Add(it);
                        }
                        else
                            skippedSet.Items.Add(it);
                    }

                    var split = Utility.SplitIDiscrete(Data, attrName);
                    double gain = initEntropy - Utility.Entropy(split, ClassificationAttributeName);
                    gain *= 1.0 * nonSkippedSet.Items.Count / Data.Items.Count;

                    foreach (var set in split)
                    {
                        var ls = new List<object>();
                        foreach (var it in set.Items)
                        {
                            var at = it.Attributes[ClassificationAttributeName];
                            var value = at.RowValue;
                            ls.Add(value);
                        }
                    }

                    if (gain > bestSplitGain)
                    {
                        bestSplitGain = gain;
                        bestSplitAttribute = attrName;
                        bestSplit = split;

                        bestSplitSkippedSet = skippedSet;
                        bestSplitNonSkippedSet = nonSkippedSet;
                    }
                }
                #endregion
            }
            #endregion

            #region gain == 0
            if (bestSplitGain == 0)
            {
                var mapping = new Dictionary<object, int>();

                foreach (var item in Data.Items)
                {
                    var attr = item.Attributes[ClassificationAttributeName];
                    var value = attr.RowValue;

                    if (!mapping.ContainsKey(value))
                        mapping[value] = 0;
                    mapping[value]++;
                }

                int maxCount = 0;
                object res = null;

                foreach (var it in mapping)
                {
                    if (it.Value > maxCount)
                    {
                        maxCount = it.Value;
                        res = it.Key;
                    }

                    Category.Result.Add(new Tuple<object, double>(it.Key, 100.0 * it.Value / Data.Items.Count));
                }
                SplitAttribute = "none";
                return;
            }
            #endregion

            #region Distribute missed items
            int leftMissedCount = bestSplitSkippedSet.Items.Count;
            int totalNonSkippedCount = bestSplitNonSkippedSet.Items.Count;
            foreach (var set in bestSplit)
            {
                int cnt = leftMissedCount * set.Items.Count / totalNonSkippedCount;
                leftMissedCount -= cnt;
                for (int i = 0; i < cnt; i++)
                {
                    int ind = bestSplitSkippedSet.Items.Count - i - 1;
                    set.Items.Add(bestSplitSkippedSet.Items[ind]);
                    bestSplitSkippedSet.Items.RemoveAt(ind);
                }
            }
            #endregion

            this.SplitAttribute = bestSplitAttribute;
            foreach (var set in bestSplit)
            {
                Node node = new Node(this.Tree, this.Height + 1);
                node.Data = set;
                node.ClassificationAttributeName = ClassificationAttributeName;
                node.SplitValue = set.Items.FirstOrDefault().Attributes[bestSplitAttribute].RowValue;
                node.Build();
                Nodes.Add(node);
            }
        }
    }

    public class Tree
    {
        //<string, Tuple<Attr type, Value type>>
        internal Dictionary<string, Tuple<AttributeType, Type>> _attributeMap = new Dictionary<string, Tuple<AttributeType, Type>>();
        public string ClassificationAttributeName { get; set; }

        public DataSet Data { get; set; }
        public Node Root { get; private set; }

        private void BuildAttributeMap()
        {
            foreach (var item in Data.Items)
            {
                foreach (var attribute in item.Attributes.Values)
                {
                    if (_attributeMap.ContainsKey(attribute.Name))
                    {
                        var tuple = _attributeMap[attribute.Name];
                        AttributeType attributeType = tuple.Item1;
                        Type valueType = tuple.Item2;


                        if (attributeType != attribute.AttributeType)
                        {
                            throw new ArgumentException($"Attribute {attribute.Name} of {item}  have different attribute types : {attributeType} != {attribute.AttributeType}");
                        }

                        if (attribute.RowValue != null && valueType != attribute.RowValue.GetType())
                        {
                            throw new ArgumentException($"Attribute {attribute.Name} of {item}  have different type of values : {valueType} != {attribute.RowValue.GetType()}");
                        }
                    }
                    else
                    {
                        _attributeMap.Add(attribute.Name, new Tuple<AttributeType, Type>(attribute.AttributeType, attribute.RowValue.GetType()));
                    }
                }
            }
        }

        public void Build()
        {
            BuildAttributeMap();
            Root = new Node(this, 0);
            Root.ClassificationAttributeName = this.ClassificationAttributeName;
            Root.Data = this.Data;
            Root.Build();
        }
    }


    public static class Utility
    {
        //split set with continuous attrbibute into to 2 parts;
        //List[0] - match
        //List[1] - no match
        public static List<DataSet> SplitContinuous(DataSet set, string attributeName, object threshold)
        {
            List<DataSet> res = new List<DataSet>();
            res.Add(new DataSet());
            res.Add(new DataSet());

            foreach (var item in set.Items)
            {
                var attr = item.Attributes[attributeName];
                var value = attr.RowValue;

                //<= threshold
                if (((IComparable)value).CompareTo(threshold) <= 0)
                {
                    res[0].Items.Add(item);
                }
                else
                {
                    res[1].Items.Add(item);
                }
            }

            return res;
        }

        //split set with discrete attrbibute into n parts( n == various values of attribute);
        public static List<DataSet> SplitIDiscrete(DataSet set, string attributeName)
        {
            List<DataSet> res = new List<DataSet>();

            var mapping = new Dictionary<object, List<Item>>();

            foreach (var item in set.Items)
            {
                var attr = item.Attributes[attributeName];
                var value = attr.RowValue;

                if (!mapping.ContainsKey(value))
                    mapping[value] = new List<Item>();
                mapping[value].Add(item);
            }


            foreach (var it in mapping)
            {
                DataSet ds = new DataSet();
                ds.Items = it.Value;
                res.Add(ds);
            }


            return res;
        }

        public static double Entropy(DataSet set, string classificationAttributeName)
        {
            //<value, count>
            var mapping = new Dictionary<object, int>();

            foreach (var item in set.Items)
            {
                var attr = item.Attributes[classificationAttributeName];
                var value = attr.RowValue;

                if (!mapping.ContainsKey(value))
                    mapping[value] = 0;
                mapping[value]++;
            }

            double res = 0;

            foreach (var it in mapping)
            {
                double p = 1.0 * it.Value / set.Items.Count;
                res += -1 * p * Math.Log(p, 2);
            }


            return res;
        }
        public static double Entropy(List<DataSet> splittingSets, string classificationAttributeName)
        {
            int totalCount = splittingSets.Aggregate<DataSet, int>(0, (cnt, ds) => cnt += ds.Items.Count);

            double sEntropy = 0;
            foreach (var data in splittingSets)
            {
                double ent = Entropy(data, classificationAttributeName);
                sEntropy += data.Items.Count * ent;
            }
            sEntropy /= totalCount;

            return sEntropy;
        }

        public static double Gain(List<DataSet> splittingSets, string classificationAttributeName)
        {
            var initialItems = splittingSets.SelectMany(ds => ds.Items).ToList();
            DataSet initialSet = new DataSet();
            initialSet.Items = initialItems;

            return Gain(initialSet, splittingSets, classificationAttributeName);
        }
        public static double Gain(DataSet initialSet, List<DataSet> splittingSets, string classificationAttributeName)
        {
            double initEntropy = Entropy(initialSet, classificationAttributeName);
            double sEntropy = Entropy(splittingSets, classificationAttributeName);
            double gain = initEntropy - sEntropy;

            return gain;
        }
    }
}
