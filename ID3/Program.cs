using ID3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DT_Algorithm;
using TreeUI;
using System.Threading;

namespace ID3
{

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            DataSet set = new DataSet();
            DataSet set1 = new DataSet();
            DataSet set2 = new DataSet();
            DataSet set3 = new DataSet();

            Item item;
            for (int i = 0; i < 4; i++)
            {
                item = new Item();
                item.Attributes.Add("1", new MyAttribute("1", i % 2));
                item.Attributes.Add("split", new MyAttribute("split", 1));
                set.Items.Add(item);
            }

            for (int i = 0; i < 4; i++)
            {
                item = new Item();
                item.Attributes.Add("1", new MyAttribute("1", i % 2));
                item.Attributes.Add("split", new MyAttribute("split", 2));
                set.Items.Add(item);
            }


            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new MyAttribute("Win", 0)); set1.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Sunny"));
            item.Attributes.Add("Win", new MyAttribute("Win", 0)); set1.Items.Add(item);


            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set2.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set2.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set2.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Overcast"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set2.Items.Add(item);

            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new MyAttribute("Win", 1)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new MyAttribute("Win", 0)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new MyAttribute("Win", 0)); set3.Items.Add(item);
            item = new Item(); item.Attributes.Add("Outlook", new MyAttribute("Outlook", "Rainy"));
            item.Attributes.Add("Win", new MyAttribute("Win", 0)); set3.Items.Add(item);

            List<DataSet> ds = new List<DataSet>() { set1, set2, set3 };

            var split = Utility.SplitIDiscrete(set, "split");
            double ent_init = Utility.Entropy(set, "1");
            double ent_split = Utility.Entropy(split, "1");
            double ent1 = Utility.Entropy(ds, "Win");

            var gain = Utility.Gain(ds, "Win");

            var ttt = Utility.SplitContinuous(set, "1", 0);

            //int _cnt = 0;
            //foreach (var dset in ttt)
            //{
            //    ++_cnt;
            //    foreach (var ditem in dset.Items)
            //    {
            //        Console.WriteLine(_cnt + ": " + ditem.Attributes["1"].RowValue);
            //    }
            //    Console.WriteLine();
            //}

            

           
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
                    var attr = new MyAttribute(columns[j], values[j]);
                    _item.Attributes.Add(attr.Name, attr);
                }
                resSet.Items.Add(_item);
                for (int j = 0; j < values.Count; j++)
                {
                    map[j].Add(values[j]);
                }
            }

            //Console.WriteLine(Utility.Entropy(resSet, "decision(category)"));

            Tree tree = new Tree();
            tree.Data = resSet;
            //tree.Data = set;
            tree.ClassificationAttributeName = "decision(category)";
           // tree.ClassificationAttributeName = "1";
            tree.Build();



            foreach (string line in lines.Skip(1))
            {
                Console.WriteLine(line);
            }

            
            
            Task task = new Task(() => {
                var ss = new MainWindow(tree);
                ss.Show();
                });
            var myThread = new Thread(new ThreadStart(() => {
                var ss = new MainWindow(tree);
                ss.ShowDialog();
            }));

            myThread.SetApartmentState(ApartmentState.STA);

            myThread.Start();

         
            //task.Start();
            //Task.WaitAll(task);
        }
    }
}
