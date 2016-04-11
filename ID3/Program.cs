using ID3;
using System;
using System.Collections.Generic;
using System.IO;
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
    public interface IItemAttribute : IComparable
    {
        string Name { get; set; }
        AttributeType AttributeType { get; }
        object RowValue { get; set; }
    }

    //class ItemAttribute<T> : IItemAttribute where T : IComparable<T>
    //{
    //    static ISet<T> allowedValues { get; set; } = new HashSet<T>();

    //    private T _value = default(T);
    //    public Type Type { get; private set; }


    //    public ItemAttribute(string name, T value, AttributeType attributeType = AttributeType.Discrete)
    //    {
    //        Type = typeof(T);
    //        this.Name = name;
    //        this.RowValue = value;
    //        this.AttributeType = attributeType;
    //    }

    //    public string Name { get; set; }
    //    public object RowValue
    //    {
    //        get
    //        {
    //            return _value;
    //        }
    //        set
    //        {
    //            _value = (T)Convert.ChangeType(value, typeof(T));
    //            allowedValues.Add(_value);
    //        }
    //    }
    //    public AttributeType AttributeType { get; set; } = AttributeType.Discrete;



    //    public int CompareTo(IItemAttribute other)
    //    {
    //        if (this._value.GetType() != other.RowValue.GetType())
    //            throw new ArgumentException("Types are not equal");

    //        return this._value.CompareTo((T)other.RowValue);
    //    }
    //    public int CompareTo(object other)
    //    {
    //        if (this._value.GetType() != other.GetType())
    //            throw new ArgumentException("Types are not equal");

    //        return this._value.CompareTo((T)other);
    //    }
    //}
    public class DiscreteAttribute<T> : IItemAttribute where T : IComparable<T>
    {
        static ISet<T> allowedValues { get; set; } = new HashSet<T>();

        private T _value = default(T);
        public Type Type { get; private set; }


        public DiscreteAttribute(string name, T value)
        {
            Type = typeof(T);
            this.Name = name;
            this.RowValue = value;
        }

        public string Name { get; set; }
        public object RowValue
        {
            get
            {
                return _value;
            }
            set
            {
                _value = (T)Convert.ChangeType(value, typeof(T));
                allowedValues.Add(_value);
            }
        }
        public AttributeType AttributeType { get; set; } = AttributeType.Discrete;



        public int CompareTo(IItemAttribute other)
        {
            if (this._value.GetType() != other.RowValue.GetType())
                throw new ArgumentException("Types are not equal");

            return this._value.CompareTo((T)other.RowValue);
        }
        public int CompareTo(object other)
        {
            if (this._value.GetType() != other.GetType())
                throw new ArgumentException("Types are not equal");

            return this._value.CompareTo((T)other);
        }
    }
    public class ContinuousAttribute : IItemAttribute
    {

        public ContinuousAttribute(string name, IComparable value)
        {
            this.Name = name;
            this.RowValue = value;
        }


        public string Name { get; set; }
        public object RowValue { get; set; }

        public AttributeType AttributeType { get; } = AttributeType.Continuous;

        public int CompareTo(IItemAttribute other)
        {
            var cattr = other as ContinuousAttribute;
            if (cattr == null) 
                throw new ArgumentException("Type is not ContinuousAttribute");

            return this.CompareTo(other.RowValue);
        }

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

    public class Node
    {
        public string ClassificationAttributeName { get; set; }
        //public Dictionary<string, IItemAttribute> AllowedAttributes { get; set; } = new Dictionary<string, IItemAttribute>();

        public object Category { get; set; }
        public string SplitAttribute { get; set; }
        public object SplitValue { get; set; }

        public Node Root { get; private set; }

        public DataSet Data { get; set; }
        public List<Node> Nodes { get; private set; } = new List<Node>();

        public void Build()
        {
            this.Build(null);
        }

        protected void Build(Node root)
        {
            double initEntropy = Utility.Entropy(Data, ClassificationAttributeName);

            //if set consist items with one category
            if (initEntropy == 0)
            {
                Category = Data.Items.FirstOrDefault().Attributes[ClassificationAttributeName].RowValue;
                return;
            }

            List<DataSet> bestSplit = null;
            string bestSplitAttribute = null;
            double bestSplitGain = 0;

            foreach (var item in Data.Items)
            {
                var values = item.Attributes.Values.ToList();
                foreach (var attr in values)
                {
                    if (attr.Name == this.ClassificationAttributeName) continue;

                    #region Continuous Attribute
                    if (attr.AttributeType == AttributeType.Continuous)
                    {
                        var set = new SortedSet<object>();

                        foreach (var it in Data.Items)
                        {
                            var at = it.Attributes[attr.Name];
                            var val = at.RowValue;

                            set.Add(val);
                        }

                        
                        foreach(var threshold in set)
                        {
                            var split = Utility.SplitContinuous(Data, attr.Name, threshold);
                            double gain = initEntropy - Utility.Entropy(split, ClassificationAttributeName);

                            if (gain > bestSplitGain)
                            {
                                bestSplitGain = gain;
                                bestSplitAttribute = attr.Name;
                                bestSplit = split;
                            }
                        }


                    }
                    #endregion

                    #region Discrete Attribute
                    if (attr.AttributeType == AttributeType.Discrete)
                    {
                        var split = Utility.SplitIDiscrete(Data, attr.Name);
                        double gain = initEntropy - Utility.Entropy(split, ClassificationAttributeName);

                        if (gain > bestSplitGain)
                        {
                            bestSplitGain = gain;
                            bestSplitAttribute = attr.Name;
                            bestSplit = split;
                        }
                    }
                    #endregion
                }
            }

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
                }
                SplitAttribute = "none";
                Category = res;
                return;
                throw new Exception("best gain == " + bestSplitGain);
            }
            Console.WriteLine(bestSplitGain);
            this.SplitAttribute = bestSplitAttribute;
            foreach (var set in bestSplit)
            {
                Node node = new Node();
                node.Data = set;
                node.ClassificationAttributeName = ClassificationAttributeName;
                node.SplitValue = set.Items.FirstOrDefault().Attributes[bestSplitAttribute].RowValue;
                Nodes.Add(node);
                node.Build(root ?? this);
            }
        }
    }
    public class Tree
    {
        public Node Root { get; set; }
    }

    public class Config
    {
        private Dictionary<string, Tuple<AttributeType, Type>> _attributeMap = new Dictionary<string, Tuple<AttributeType, Type>>();
        public Dictionary<Tree, Dictionary<string, Tuple<AttributeType, Type>>> _treeMap = new Dictionary<Tree, Dictionary<string, Tuple<AttributeType, Type>>>();


        public Config()
        {

        }

        public bool RegisterAttribute(string attributeName, AttributeType attributeType, Type valueType)
        {
            if (_attributeMap.ContainsKey(attributeName))
            {
                return false;
            }

            _attributeMap[attributeName] = new Tuple<AttributeType, Type>(attributeType, valueType);
            return true;
        }

        public Test test { get; set; }

        public class Test
        {
            public Test()
            {
                
            }
            public void Temp()
            {

            }
        }
    }

    static class Utility
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


            foreach(var it in mapping)
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

    class Program
    {
        static void Main(string[] args) 
        {
            Config tree = new Config();
            Config.Test a = new Config.Test();

            DataSet set = new DataSet();
            DataSet set1 = new DataSet();
            DataSet set2 = new DataSet();
            DataSet set3 = new DataSet();

            Item item;
            for (int i = 0; i < 3; i++)
            {
                item = new Item();
                item.Attributes.Add("1", new DiscreteAttribute<int>("1", i));
                set.Items.Add(item);
            }

            for (int i = 0; i < 2; i++)
            {
                item = new Item();
                item.Attributes.Add("1", new DiscreteAttribute<int>("1", i + 10));
                set.Items.Add(item);
            }

           
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 0)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 0)); set1.Items.Add(item);


            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set2.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set2.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set2.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set2.Items.Add(item);

            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 1)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 0)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 0)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new ContinuousAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new ContinuousAttribute("Win", 0)); set3.Items.Add(item);

            List<DataSet> ds = new List<DataSet>() { set1, set2, set3 };

            double ent = Utility.Entropy(set, "1");
            double ent1 = Utility.Entropy(ds, "Win");

            var gain = Utility.Gain(ds, "Win");

            var ttt = Utility.SplitContinuous(set, "1", 0);

            int _cnt = 0;
            foreach(var dset in ttt)
            {
                ++_cnt;
                foreach(var ditem in dset.Items)
                {
                    Console.WriteLine(_cnt + ": " + ditem.Attributes["1"].RowValue);
                }
                Console.WriteLine();
            }

            double res = 3.0 / 5.0 * Math.Log(5.0 / 3.0, 2);
            res += 2.0 / 5.0 * Math.Log(5.0 / 2.0, 2);


            //var mapping = new Dictionary<IItemAttribute, int>();
            //mapping.Add(new ItemAttribute<int>("2", 2), 2);
            //mapping.Add(new ItemAttribute<int>("1", 1), 1);

            var tt = new ContinuousAttribute("2", 2);
            var tt1 = new ContinuousAttribute("1", 1);
            //var res = tt.CompareTo(tt1);
            //foreach (var it in mapping)
            //{
            //    Console.WriteLine(it);
            //}
            ////var att = new ItemAttribute<int>(5);
            //att.Value = "45";
            //object two = att.Value;
            var lines = File.ReadLines("input.txt").ToList();

            var columns = lines[0].Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.ToLower()).ToList();
            var map = new HashSet<string>[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                map[i] = new HashSet<string>();
            }

            DataSet resSet = new DataSet();
            for (int i = 1; i < lines.Count; i++)
            {
                var values = lines[i].Split(new[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.ToLower()).ToList();

                if (values.Count != columns.Count)
                {
                    throw new ArgumentException("Count of values in row are not equal to columns count");
                }

                Item _item = new Item();
                for (int j = 0; j < values.Count; j++)
                {
                    DiscreteAttribute<string> attr = new DiscreteAttribute<string>(columns[j], values[j]);
                    _item.Attributes.Add(attr.Name, attr);
                }
                resSet.Items.Add(_item);
                for (int j = 0; j < values.Count; j++)
                {
                    map[j].Add(values[j]);
                }
            }

            //Console.WriteLine(Utility.Entropy(resSet, "decision(category)"));

            Node root = new Node();
            root.Data = resSet;
            root.ClassificationAttributeName = "decision(category)";
            root.Build();

            foreach (string line in lines.Skip(1))
            {
                Console.WriteLine(line);
            }

        }
    }
}
