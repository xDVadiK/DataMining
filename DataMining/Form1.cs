using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace DataMining
{
    public partial class Form1 : Form
    {
        string titanicData;
        string predictData;
        string submissionData;

        string filePath;
        StreamWriter writer;

        public Form1()
        {
            InitializeComponent();
            titanicData = "D:\\titanic\\train.csv";
            textBox1.Text = titanicData;
            predictData = "D:\\titanic\\test.csv";
            textBox2.Text = predictData;
            submissionData = "D:\\titanic\\gender_submission.csv";
            textBox3.Text = submissionData;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                List<TitanicData> records = ReadTitanicDataFromCSV(titanicData, "train");

                List<TitanicDataInput> trainingData = SelectInput(records);

                List<TitanicDataOutput> labels = SelectOutput(records);

                List<TitanicData> toPredict = ReadTitanicDataFromCSV(predictData, "test");

                List<TitanicDataInput> predictInput = SelectInput(toPredict);

                NaiveBayesClassifier naiveBayesClassifier = new NaiveBayesClassifier();
                naiveBayesClassifier.Train(trainingData, labels);

                List<TitanicDataOutput> prediction = naiveBayesClassifier.Predict(predictInput);

                List<TitanicDataOutput> submission = ReadSubmissionFromCSV(submissionData);

                if (checkBox1.Checked)
                {
                    writer = new StreamWriter(filePath);
                    writer.WriteLine("PassengerId, Pediction, Real");
                }

                listView1.Items.Clear();
                for (int i = 0; i < prediction.Count; i++)
                {
                    ListViewItem item = new ListViewItem(prediction.ElementAt(i).PassengerId.ToString());
                    item.SubItems.Add(prediction.ElementAt(i).Survived.ToString());
                    item.SubItems.Add(submission.ElementAt(i).Survived.ToString());
                    listView1.Items.Add(item);
                    if(checkBox1.Checked)
                    {
                        writer.WriteLine(prediction.ElementAt(i).PassengerId + ", " + prediction.ElementAt(i).Survived + ", " + submission.ElementAt(i).Survived);
                    }
                }

                if (checkBox1.Checked)
                {
                    writer.Close();
                }

                textBox4.Text = CalculateAccuracy(prediction, submission).ToString();
            }
            catch(Exception exp)
            {
                MessageBox.Show(exp.Message, "Exception");
            }
        }

        // Reading passenger information from a file
        private List<TitanicData> ReadTitanicDataFromCSV(string filePath, string type)
        {
            List<TitanicData> records = new List<TitanicData>(); // Create a passenger list

            using (StreamReader reader = new StreamReader(filePath))
            {
                reader.ReadLine();

                switch (type)
                {
                    case "train":
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            string[] values = line.Split(',');

                            if (values.Length == 13)
                            {
                                TitanicData data = new TitanicData
                                {
                                    PassengerId = int.Parse(values[0]),
                                    Survived = int.Parse(values[1]),
                                    Pclass = int.Parse(values[2]),
                                    Sex = values[5].Trim().Equals("female") ? 0 : 1,
                                };

                                if (float.TryParse(values[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float age))
                                {
                                    data.Age = age;
                                }
                                else
                                {
                                    data.Age = null;
                                }

                                if (float.TryParse(values[10], NumberStyles.Float, CultureInfo.InvariantCulture, out float fare))
                                {
                                    data.Fare = fare;
                                }
                                else
                                {
                                    data.Fare = null;
                                }

                                records.Add(data);
                            }
                        }
                        return ReplaceNulls(records);
                    case "test":
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            string[] values = line.Split(',');

                            if (values.Length == 12)
                            {
                                TitanicData data = new TitanicData
                                {
                                    PassengerId = int.Parse(values[0]),
                                    Survived = -1,
                                    Pclass = int.Parse(values[1]),
                                    Sex = values[4].Trim().Equals("female") ? 0 : 1,
                                };

                                if (float.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float age))
                                {
                                    data.Age = age;
                                }
                                else
                                {
                                    data.Age = null;
                                }

                                if (float.TryParse(values[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float fare))
                                {
                                    data.Fare = fare;
                                }
                                else
                                {
                                    data.Fare = null;
                                }

                                records.Add(data);
                            }
                        }
                        return ReplaceNulls(records);
                    default:
                        throw new InvalidOperationException("Invalid file content!");
                }
            }
        }

        // Replacing null values with mean values
        private List<TitanicData> ReplaceNulls(List<TitanicData> titanicDatas)
        {
            float? averageAge = titanicDatas
            .Where(data => data.Age.HasValue)
            .Average(data => data.Age);

            float? averageFare = titanicDatas
            .Where(data => data.Fare.HasValue)
            .Average(data => data.Fare);

            foreach (var data in titanicDatas)
            {
                if (!data.Age.HasValue)
                {
                    data.Age = averageAge;
                }

                if (!data.Fare.HasValue)
                {
                    data.Fare = averageFare;
                }
            }

            return titanicDatas;
        }

        // Creating a set of data fed to the model input
        private List<TitanicDataInput> SelectInput(List<TitanicData> records)
        {
            return records
                .Select(data => new TitanicDataInput
                {
                    PassengerId = data.PassengerId,
                    Pclass = data.Pclass,
                    Sex = data.Sex,
                    Age = data.Age,
                    Fare = data.Fare
                })
            .ToList();
        }

        // Creating a dataset containing labelled information
        private List<TitanicDataOutput> SelectOutput(List<TitanicData> records)
        {
            return records
                .Select(data => new TitanicDataOutput
                {
                    PassengerId = data.PassengerId,
                    Survived = data.Survived
                })
            .ToList();
        }

        // Reading information to validate model predictions
        private List<TitanicDataOutput> ReadSubmissionFromCSV(string filePath)
        {
            List<TitanicDataOutput> records = new List<TitanicDataOutput>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] values = line.Split(',');

                    if (values.Length == 2)
                    {
                        TitanicDataOutput data = new TitanicDataOutput
                        {
                            PassengerId = int.Parse(values[0]),
                            Survived = int.Parse(values[1]),
                        };

                        records.Add(data);
                    }
                }
                return records;
            }
        }

        // Calculating the prediction accuracy of the model
        private double CalculateAccuracy(List<TitanicDataOutput> prediction, List<TitanicDataOutput> submission)
        {
            double accuracy = 0;

            if (prediction.Count == submission.Count)
            {
                for (int i = 0; i < prediction.Count; i++)
                {
                    if (prediction[i].PassengerId == submission[i].PassengerId && prediction[i].Survived == submission[i].Survived)
                    {
                        accuracy++;
                    }
                }
                return (double)accuracy / prediction.Count;
            }
            else
            {
                throw new InvalidOperationException("Invalid list sizes!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select File";
            openFileDialog.Filter = "CSV Files(*.csv)|*.csv";
            openFileDialog.Multiselect = false;
            openFileDialog.ShowDialog();
            textBox1.Text = openFileDialog.FileName;
            titanicData = openFileDialog.FileName;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select File";
            openFileDialog.Filter = "CSV Files(*.csv)|*.csv";
            openFileDialog.Multiselect = false;
            openFileDialog.ShowDialog();
            textBox2.Text = openFileDialog.FileName;
            predictData = openFileDialog.FileName;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select File";
            openFileDialog.Filter = "CSV Files(*.csv)|*.csv";
            openFileDialog.ShowDialog();
            openFileDialog.Multiselect = false;
            textBox3.Text = openFileDialog.FileName; 
            submissionData = openFileDialog.FileName;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select a folder to save the file.";

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = folderBrowserDialog.SelectedPath;
                filePath += filePath.EndsWith("\\") ? "result.csv" : "\\result.csv";
                textBox5.Text = filePath;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                label8.Visible = true; 
                label9.Visible = true;
                textBox5.Visible = true;
                button5.Visible = true;
            }
            else
            {
                label8.Visible = false;
                label9.Visible = false;
                textBox5.Visible = false;
                button5.Visible = false;
            }
        }
    }
}