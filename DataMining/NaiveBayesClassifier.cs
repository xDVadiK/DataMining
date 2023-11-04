using System;
using System.Collections.Generic;
using System.Linq;

namespace DataMining
{
    internal class NaiveBayesClassifier
    {
        private Dictionary<int, Dictionary<int, Dictionary<int, double>>> likelihoods; // Conditional probabilities
        private Dictionary<int, double> classProbabilities; // Priori probabilities

        public NaiveBayesClassifier()
        {
            likelihoods = new Dictionary<int, Dictionary<int, Dictionary<int, double>>>();
            classProbabilities = new Dictionary<int, double>();
        }

        // Model training
        public void Train(List<TitanicDataInput> trainingData, List<TitanicDataOutput> labels)
        {
            int numSamples = trainingData.Count;
            int numFeatures = typeof(TitanicDataInput).GetProperties().Length;
            List<int> uniqueClasses = labels.Select(label => label.Survived).Distinct().ToList();

            // Вычисление априорных вероятностей и объявление условных
            foreach (var cls in uniqueClasses)
            {
                classProbabilities[cls] = labels.Count(label => label.Survived == cls) / (double)numSamples;
                likelihoods[cls] = new Dictionary<int, Dictionary<int, double>>();
                for (int featureIndex = 0; featureIndex < numFeatures; featureIndex++)
                {
                    likelihoods[cls][featureIndex] = new Dictionary<int, double>();
                }
            }

            // Вычисление условных вероятностей
            for (int featureIndex = 0; featureIndex < numFeatures - 1; featureIndex++)
            {
                for (int clsIndex = 0; clsIndex < uniqueClasses.Count; clsIndex++)
                {
                    var cls = uniqueClasses[clsIndex];
                    var featureValues = trainingData.Select(data => GetFeatureValue(data, featureIndex)).Distinct().ToList();
                    foreach (var value in featureValues)
                    {
                        likelihoods[cls][featureIndex][value] = (trainingData.Count(data => GetFeatureValue(data, featureIndex) == value && labels[data.PassengerId - 1].Survived == cls) + 1) /
                            (double)((labels.Count(label => label.Survived == cls) + featureValues.Count) + 1);
                    }
                }
            }
        }

        // Predicting the outcome for the test sample
        public List<TitanicDataOutput> Predict(List<TitanicDataInput> data)
        {
            List<TitanicDataOutput> titanicSurvivedPredicrions = new List<TitanicDataOutput>();

            foreach (TitanicDataInput passenger in data)
            {
                var uniqueClasses = classProbabilities.Keys.ToList();
                double bestClass = -1;
                double bestProb = double.MinValue;

                foreach (var cls in uniqueClasses)
                {
                    double classProb = classProbabilities[cls];

                    for (int featureIndex = 0; featureIndex < likelihoods[cls].Count - 1; featureIndex++)
                    {
                        var featureValue = GetFeatureValue(passenger, featureIndex);
                        if (likelihoods[cls][featureIndex].ContainsKey(featureValue))
                        {
                            classProb += likelihoods[cls][featureIndex][featureValue];
                        }
                    }

                    if (classProb > bestProb)
                    {
                        bestClass = cls;
                        bestProb = classProb;
                    }
                }

                titanicSurvivedPredicrions.Add(new TitanicDataOutput { PassengerId = passenger.PassengerId, Survived = (int)bestClass });
            }

            return titanicSurvivedPredicrions;
        }

        // Obtaining the value of the parameter
        private static int GetFeatureValue(TitanicDataInput data, int featureIndex)
        {
            switch (featureIndex) 
            {
                case 0:
                    return data.Pclass;
                case 1:
                    return data.Sex;
                case 2:
                    return (int)data.Age;
                case 3:
                    return (int)data.Fare;
                default:
                    throw new InvalidOperationException("Invalid feature index.");
            }
        }
    }
}