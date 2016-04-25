using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ID3
{
    public enum AttributeType
    {
        Discrete,
        Continuous
    }
    public interface IItemAttribute
    {
        string Name { get; set; }
        AttributeType AttributeType { get; }
        IComparable Value { get; set; }
    }

    public class MyAttribute : IItemAttribute
    {
        public string Name { get; set; }
        public IComparable Value { get; set; }
        public AttributeType AttributeType { get; set; } = AttributeType.Discrete;

        public MyAttribute(string name, IComparable value)
        {
            this.Name = name;
            this.Value = value;
        }

        public MyAttribute(string name, IComparable value, AttributeType attributeType)
        {
            this.Name = name;
            this.Value = value;
            this.AttributeType = attributeType;
        }
    }

    public class Item
    {
        public Dictionary<string, IItemAttribute> Attributes { get; set; } = new Dictionary<string, IItemAttribute>();

        public override string ToString()
        {
            string res = "";

            int pad = 10;
            foreach (var attr in Attributes.Values)
            {
                res += attr.Name.PadRight(pad);
            }
            res += Environment.NewLine;

            foreach (var attr in Attributes.Values)
            {
                res += attr.Value.ToString().PadRight(pad);
            }

            return res;
        }
    }
    public class DataSet
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public void Print(int pad)
        {
            foreach (var attribute in Items[0].Attributes.Values)
            {
                Console.Write(attribute.Name.PadRight(pad));
            }
            Console.WriteLine();
            foreach (Item item in Items)
            {
                foreach (var attribute in item.Attributes.Values)
                {
                    Console.Write(attribute.Value?.ToString().PadRight(pad));
                }
                Console.WriteLine();
            }
        }
    }


    public class Classification
    {
        private List<ClassificationItem> result = new List<ClassificationItem>();
        public List<ClassificationItem> Result
        {
            get
            {
                result.Sort();
                result.Reverse();
                return result;
            }
            set
            {
                result = value;
            }
        }

        public override string ToString()
        {
            string res = "";
            Result.Sort();
            Result.Reverse();
            foreach (var citem in Result)
            {
                res += citem.ToString() + "\n";
            }
            return res;
        }
    }
    public class ClassificationItem : IComparable<ClassificationItem>
    {
        public object Value { get; set; }
        public double Percent { get; set; }

        public double Count { get; set; }

        public int CompareTo(ClassificationItem other)
        {
            return this.Percent.CompareTo(other.Percent);
        }
        public override string ToString()
        {
            return $"{Value} : {Percent} : {Count}";
        }
    }


    public class Node
    {
        public Tree Tree { get; }
        public Node(Tree tree, int height)
        {
            this.Tree = tree;
            this.Height = height;
        }

        public Classification Classification { get; set; } = new Classification();
        public string SplitAttributeName { get; set; }
        public IComparable SplitValue { get; set; }
        public IComparable SplitValueThreshold { get; set; }
        public int SplitValueThresholdSign { get; set; } = 1;


        public DataSet Data { get; set; }
        public List<Node> Nodes { get; private set; } = new List<Node>();

        public int Height { get; }


        private bool _crossValidate = false;
        public bool EnableCrossValidate
        {
            get
            {
                return this._crossValidate;
            }
            set
            {
                this._crossValidate = value;
                foreach (Node node in this.Nodes)
                    node.EnableCrossValidate = this._crossValidate;
            }
        }

        public int RightValidate { get; set; } = 0;
        public int WrongValidate { get; set; } = 0;


        private void ClassifyNode()
        {
            if (this.Nodes.Count == 0)
            {
                Classification.Result.Add(new ClassificationItem()
                {
                    Value = Data.Items.FirstOrDefault().Attributes[Tree.ClassificationAttributeName].Value,
                    Percent = 100,
                    Count = Data.Items.Count
                });
            }
            else
            {
                var map = new Dictionary<object, double>();
                foreach (var item in this.Data.Items)
                {
                    if (item.Attributes.ContainsKey(Tree.ClassificationAttributeName))
                    {
                        object value = item.Attributes[Tree.ClassificationAttributeName].Value;
                        if (!map.ContainsKey(value))
                            map[value] = 0;
                        map[value]++;
                    }
                }

                double total = map.Values.Sum();
                foreach (var pair in map)
                {
                    object value = pair.Key;
                    double count = pair.Value;
                    this.Classification.Result.Add(new ClassificationItem()
                    {
                        Value = value,
                        Percent = 100.0 * count / total,
                        Count = count
                    });
                }
            }
        }
        public void Build()
        {
            double initEntropy = Utility.Entropy(Data, Tree.ClassificationAttributeName);

            //if set consist items with one category
            if (initEntropy == 0)
            {
                ClassifyNode();
                return;
            }

            DataSet bestSplitSkippedSet = null;
            DataSet bestSplitNonSkippedSet = null;

            List<DataSet> bestSplit = null;
            string bestSplitAttribute = null;
            double bestSplitGain = 0;

            IComparable bestSplitThreshold = null;

            #region Split and count best gain
            foreach (var attr in Tree._attributeMap)
            {
                DataSet skippedSet = new DataSet();
                DataSet nonSkippedSet = new DataSet();

                string attrName = attr.Key;
                AttributeType attrType = attr.Value.Item1;

                if (attrName == Tree.ClassificationAttributeName) continue;

                #region Continuous Attribute
                if (attrType == AttributeType.Continuous)
                {
                    var set = new SortedSet<IComparable>();

                    foreach (var it in Data.Items)
                    {
                        IItemAttribute at = null;
                        if (it.Attributes.TryGetValue(attrName, out at))
                        {
                            var val = at.Value;
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
                        double gain = initEntropy - Utility.Entropy(split, Tree.ClassificationAttributeName);

                        if (gain > bestSplitGain)
                        {
                            bestSplitGain = gain;
                            bestSplitAttribute = attrName;
                            bestSplit = split;

                            bestSplitSkippedSet = skippedSet;
                            bestSplitNonSkippedSet = nonSkippedSet;

                            bestSplitThreshold = threshold;
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
                            var val = at.Value;
                            if (val != null)
                                nonSkippedSet.Items.Add(it);
                            else
                                skippedSet.Items.Add(it);
                        }
                        else
                            skippedSet.Items.Add(it);
                    }

                    var split = Utility.SplitIDiscrete(Data, attrName);
                    double gain = initEntropy - Utility.Entropy(split, Tree.ClassificationAttributeName);
                    gain *= 1.0 * nonSkippedSet.Items.Count / Data.Items.Count;

                    foreach (var set in split)
                    {
                        var ls = new List<object>();
                        foreach (var it in set.Items)
                        {
                            var at = it.Attributes[Tree.ClassificationAttributeName];
                            var value = at.Value;
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
                    var attr = item.Attributes[Tree.ClassificationAttributeName];
                    var value = attr.Value;

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

                    Classification.Result.Add(new ClassificationItem()
                    {
                        Value = it.Key,
                        Percent = 100.0 * it.Value / Data.Items.Count,
                        Count = it.Value
                    });
                }
                SplitAttributeName = "none";
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

            this.SplitAttributeName = bestSplitAttribute;

            if (Tree._attributeMap[this.SplitAttributeName].Item1 == AttributeType.Discrete)
            {
                foreach (var set in bestSplit)
                {
                    Node node = new Node(this.Tree, this.Height + 1);
                    node.Data = set;
                    node.SplitValue = set.Items.FirstOrDefault().Attributes[this.SplitAttributeName].Value;
                    node.Build();
                    Nodes.Add(node);
                }
            }
            else
            {
                int count = -1;
                foreach (var set in bestSplit)
                {
                    Node node = new Node(this.Tree, this.Height + 1);
                    node.Data = set;
                    node.SplitValueThreshold = bestSplitThreshold;
                    node.SplitValueThresholdSign = count;
                    node.Build();
                    Nodes.Add(node);
                    count += 2;
                }
            }
            ClassifyNode();
        }

        public Classification Classify(Item item)
        {
            if (this.Nodes.Count == 0)
            {
                if (EnableCrossValidate)
                {
                    object value = this.Classification.Result.FirstOrDefault()?.Value;
                    if (value == null)
                        throw new NullReferenceException();

                    if (item.Attributes[Tree.ClassificationAttributeName].Value.Equals(value))
                        this.RightValidate++;
                    else
                        this.WrongValidate++;
                }
                if (this.Classification.Result.Sum(x => x.Percent) != 100)
                {

                }
                return this.Classification;
            }

            var attr = item.Attributes[this.SplitAttributeName];
            if (attr.Value == null)
            {
                var map = new Dictionary<object, double>();
                foreach (var node in this.Nodes)
                {
                    var cls = node.Classify(item);
                    foreach (var clsItem in cls.Result)
                    {
                        object value = clsItem.Value;
                        double count = clsItem.Count;

                        if (!map.ContainsKey(value))
                            map[value] = 0;
                        map[value] += count;
                    }
                }

                double total = map.Values.Sum();
                Classification res = new Classification();
                foreach (var pair in map)
                {
                    object value = pair.Key;
                    double count = pair.Value;
                    res.Result.Add(new ClassificationItem()
                    {
                        Value = value,
                        Percent = 100.0 * count / total,
                        Count = count
                    });
                }
                return res;
            }
            else
            {

                Node node;
                if (attr.AttributeType == AttributeType.Discrete)
                {
                    node = this.Nodes.FirstOrDefault(n => n.SplitValue.CompareTo(attr.Value) == 0);
                }
                else
                {
                    node = this.Nodes.FirstOrDefault(n =>
                    {
                        var _item = n.Data.Items.Where(x => x.Attributes[this.SplitAttributeName] != null).FirstOrDefault();
                        if (n.SplitValueThresholdSign == -1)
                        {
                            return attr.Value.CompareTo(n.SplitValueThreshold) <= 0;
                        }
                        else
                        {
                            return attr.Value.CompareTo(n.SplitValueThreshold) > 0;
                        }
                    });
                }
                if (node != null)
                {
                    var res = node.Classify(item);
                    var summ = res.Result.Sum(x => x.Percent);
                    if (summ != 100)
                    {

                    }
                    return res;
                }
                else
                {
                    var summ = this.Classification.Result.Sum(x => x.Percent);
                    if (summ != 100)
                    {

                    }
                    return this.Classification;
                }
            }
        }



        public double Prunning(double z)
        {
            if (this.Nodes.Count == 0)
            {
                double n = WrongValidate + RightValidate;
                double f = 1.0 * WrongValidate / n;
                //p = f +- delt
                //delt = z*sqrt( f*(1-f) / N )
                double delt = z * Math.Sqrt(f * (1 - f) / n);
                double up = f + delt;

                if (n == 0)
                    return 0;
                return up;
            }
            {
                double totalError = 0;
                int right = 0;
                int wrong = 0;
                foreach (Node node in this.Nodes)
                {
                    double t = node.Prunning(z);
                    totalError += t;

                    right += node.RightValidate;
                    wrong += node.WrongValidate;
                }
                double averageError = totalError / this.Nodes.Count;

                double n = wrong + right;
                double f = 1.0 * wrong / n;
                //p = f +- delt
                //delt = z*sqrt( f*(1-f) / N )
                double delt = z * Math.Sqrt(f * (1 - f) / n);
                double up = f + delt;

                if (n == 0)
                    up = 0;

                this.WrongValidate = wrong;
                this.RightValidate = right;

                if (up <= averageError)
                {
                    //pruning node
                    this.Nodes.Clear();
                    return up;
                }
                else
                {
                    //no
                    return averageError;
                }
            }
        }
    }

    public class Tree
    {
        //<string, Tuple<Attr type, Value type>>
        internal Dictionary<string, Tuple<AttributeType, Type>> _attributeMap = new Dictionary<string, Tuple<AttributeType, Type>>();

        private string _classificationAttributeName;
        public string ClassificationAttributeName
        {
            get
            {
                return _classificationAttributeName;
            }
            set
            {
                _classificationAttributeName = value?.ToLower();
            }
        }

        public DataSet Data { get; set; }
        public Node Root { get; private set; }
        private bool _crossValidate = false;
        public bool EnableCrossValidate
        {
            get
            {
                return this._crossValidate;
            }
            set
            {
                this._crossValidate = value;
                Root.EnableCrossValidate = this._crossValidate;
            }
        }


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

                        if (attribute.Value != null && valueType != attribute.Value.GetType())
                        {
                            throw new ArgumentException($"Attribute {attribute.Name} of {item}  have different type of values : {valueType} != {attribute.Value.GetType()}");
                        }
                    }
                    else
                    {
                        _attributeMap.Add(attribute.Name, new Tuple<AttributeType, Type>(attribute.AttributeType, attribute.Value.GetType()));
                    }
                }
            }
        }

        public void Build()
        {
            BuildAttributeMap();
            Root = new Node(this, 0);
            Root.Data = this.Data;
            Root.Build();
        }

        public Classification Classify(Item item)
        {
            var res = Root.Classify(item);
            return res;
        }


        public void Prunning(double z)
        {
            Root.Prunning(z);
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
                var value = attr.Value;

                //<= threshold
                if (value.CompareTo(threshold) <= 0)
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
                var value = attr.Value;

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
                var value = attr.Value;

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
