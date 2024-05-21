using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using MathNet.Numerics.Distributions;

namespace ValoLibrary
{
    public class CDOModel
    {
        public const double ShockMultiplier = 1.05;
        public const double Shock = 0.05;

        public static double[,] Recursion(int numberOfIssuer, double[] defaultProbKnowingFactor, int? maxRequest = null, bool withGreeks = false)
        {
            int forcedMaxRequest;
            if (!maxRequest.HasValue)
            {
                forcedMaxRequest = numberOfIssuer - 1;
            }
            else
            {
                forcedMaxRequest = Math.Min(maxRequest.Value, numberOfIssuer - 1);
            }

            double[,] distrib;
            if (withGreeks)
            {
                distrib = new double[forcedMaxRequest + 2, numberOfIssuer + 1];
            }
            else
            {
                distrib = new double[forcedMaxRequest + 2, 1];
            }

            for (int i = 1; i <= forcedMaxRequest + 1; i++)
            {
                distrib[i, 0] = 0;
            }

            distrib[0, 0] = 1;

            for (int i = 1; i <= numberOfIssuer; i++)
            {
                int jstart = i > forcedMaxRequest + 1 ? forcedMaxRequest + 1 : i;
                double p = defaultProbKnowingFactor[i - 1];
                double omp = 1 - p;

                for (int j = jstart; j >= 1; j--)
                {
                    distrib[j, 0] = distrib[j - 1, 0] * p + distrib[j, 0] * omp;
                }

                distrib[0, 0] = distrib[0, 0] * omp;
            }

            if (withGreeks)
            {
                for (int i = 1; i <= numberOfIssuer; i++)
                {
                    double p = defaultProbKnowingFactor[i - 1];
                    double omp = 1 - p;
                    distrib[0, i] = distrib[0, 0] / omp;

                    for (int j = 1; j <= forcedMaxRequest; j++)
                    {
                        distrib[j, i] = (distrib[j, 0] - distrib[j - 1, i] * p) / omp;

                        if (distrib[j, i] < 0)
                        {
                            distrib[j, i] = distrib[j + 1, 0];
                            distrib[j - 1, i] = (distrib[j, 0] - omp * distrib[j, i]) / p;
                        }
                    }

                    for (int j = forcedMaxRequest; j >= 1; j--)
                    {
                        distrib[j, i] = distrib[j - 1, i] - distrib[j, i];
                    }

                    distrib[0, i] = -distrib[0, i];
                }
            }

            return distrib;
        }

        public static double[,] GetDefaultDistribution(int numberOfIssuer, double[] defaultProbability, double[] betaVector,
            int? maxRequest = null, double[] inputThreshold = null, int? factorIndex = null, bool withGreeks = false)
        {
            double[,] defaultDistribKnowingFactor;
            double[,] defaultDistrib;
            double[] defaultThreshold = new double[numberOfIssuer];
            double[] defaultProbKnowingFactor = new double[numberOfIssuer + 1];
            double[] dp_dProb = null;
            double[] dp_dBeta = null;
            double[] dDefaultThreshold = null;

            int factorCounter;
            int nbOfGaussHermitePoints = 64;
            double[] gaussHermiteAbscissa = new double[]

            {
                -10.5261231679605, -9.89528758682953, -9.37315954964672, -8.90724909996476, -8.47752908337986, -8.07368728501022, -7.68954016404049, -7.32101303278094,
                -6.9652411205511, -6.62011226263602, -6.28401122877482, -5.95566632679948, -5.63405216434997, -5.31832522463327, -5.00777960219876, -4.70181564740749,
                -4.39991716822813, -4.10163447456665, -3.80657151394536, -3.5143759357409, -3.22473129199203, -2.93735082300462, -2.65197243543063, -2.3683545886324,
                -2.08627287988176, -1.80551717146554, -1.52588914020986, -1.24720015694311, -0.969269423071178, -0.691922305810044, -0.414988824121078, -0.138302244987009,
                 0.138302244987009, 0.414988824121078, 0.691922305810044, 0.969269423071178, 1.24720015694311, 1.52588914020986, 1.80551717146554, 2.08627287988176,
                 2.3683545886324, 2.65197243543063, 2.93735082300462, 3.22473129199203, 3.5143759357409, 3.80657151394536, 4.10163447456665, 4.39991716822813,
                 4.70181564740749, 5.00777960219876, 5.31832522463327, 5.63405216434997, 5.95566632679948, 6.28401122877482, 6.62011226263602, 6.9652411205511,
                 7.32101303278094, 7.68954016404049, 8.07368728501022, 8.47752908337986, 8.90724909996476, 9.37315954964672, 9.89528758682953, 10.5261231679605
            };
            double[] gaussHermiteWeight = new double[]
            {
                2.53418710060459E-25, 1.22602411040096E-22, 1.63228017722665E-20, 1.05092412334527E-18, 4.10618806999281E-17, 1.09871222523276E-15, 2.16838907840016E-14, 3.31758747989528E-13,
                4.07710009958856E-12, 4.13239336761409E-11, 3.5254084284956E-10, 2.57250350487446E-09, 1.62656661863386E-08, 9.00688368649578E-08, 4.40658777599536E-07, 1.91902972761357E-06,
                7.48598005351949E-06, 2.62991006123899E-05, 8.3592377852976E-05, 2.41356276405779E-04, 6.35207286288721E-04, 1.52839476722543E-03, 3.37087743793626E-03, 6.82981749780981E-03,
                1.27370763383524E-02, 2.18997473380829E-02, 3.47633024351237E-02, 5.10056393070224E-02, 6.92371821856065E-02, 8.70172186157688E-02, 0.101310028539473, 0.109304305522575,
                0.109304305522575, 0.101310028539473, 8.70172186157688E-02, 6.92371821856065E-02, 5.10056393070224E-02, 3.47633024351237E-02, 2.18997473380829E-02, 1.27370763383524E-02,
                6.82981749780981E-03, 3.37087743793626E-03, 1.52839476722543E-03, 6.35207286288721E-04, 2.41356276405779E-04, 8.3592377852976E-05, 2.62991006123899E-05, 7.48598005351949E-06,
                1.91902972761357E-06, 4.40658777599536E-07, 9.00688368649578E-08, 1.62656661863386E-08, 2.57250350487446E-09, 3.5254084284956E-10, 4.13239336761409E-11, 4.07710009958856E-12,
                3.31758747989528E-13, 2.16838907840016E-14, 1.09871222523276E-15, 4.10618806999281E-17, 1.05092412334527E-18, 1.63228017722665E-20, 1.22602411040096E-22, 2.53418710060459E-25
            };

            double remainingWeights;

            int forcedMaxRequest;

            if (!maxRequest.HasValue)
            {
                forcedMaxRequest = numberOfIssuer;
            }
            else
            {
                forcedMaxRequest = Math.Min(maxRequest.Value, numberOfIssuer);
            }

            if (!withGreeks)
            {
                defaultDistrib = new double[forcedMaxRequest + 2, 1];
            }
            else
            {
                defaultDistrib = new double[forcedMaxRequest + 2, 2 * numberOfIssuer + 1];
                dp_dProb = new double[numberOfIssuer];
                dp_dBeta = new double[numberOfIssuer];
                dDefaultThreshold = new double[numberOfIssuer];
            }

            for (int issuerCounter = 0; issuerCounter <= forcedMaxRequest; issuerCounter++)
            {
                defaultDistrib[issuerCounter, 0] = 0;

                if (withGreeks)
                {
                    for (int j = 1; j <= numberOfIssuer; j++)
                    {
                        defaultDistrib[issuerCounter, j] = 0;
                        defaultDistrib[issuerCounter, j + numberOfIssuer] = 0;
                    }
                }
            }

            if (inputThreshold == null)
            {
                for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                {
                    if (defaultProbability[issuerCounter] == 0)
                    {
                        defaultThreshold[issuerCounter] = -100;
                    }
                    else
                    {
                        defaultThreshold[issuerCounter] = Normal.InvCDF(0, 1, defaultProbability[issuerCounter]);
                    }
                }

                if (withGreeks)
                {
                    for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                    {
                        if (defaultProbability[issuerCounter] * 1.05 >= 1)
                        {
                            Console.WriteLine($"Can't shock default probability of issuer #{issuerCounter}");
                            Console.WriteLine("Delta will be wrong");
                            dDefaultThreshold[issuerCounter] = defaultThreshold[issuerCounter];
                        }
                        else
                        {
                            dDefaultThreshold[issuerCounter] = Normal.InvCDF(0, 1, defaultProbability[issuerCounter] * 1.05);
                        }
                    }
                }
            }
            else
            {
                defaultThreshold = inputThreshold;
            }

            defaultDistrib[forcedMaxRequest + 1, 0] = 0;

            double[] remainingWeight = new double[] { 0 };

            for (int piece = 1; piece <= 2; piece++)
            {
                int factorStartIndex, factorEndIndex, factorStep = -1;

                if (!factorIndex.HasValue)
                {
                    if (piece == 1)
                    {
                        factorStartIndex = nbOfGaussHermitePoints / 2;
                        factorEndIndex = nbOfGaussHermitePoints - 1;
                        factorStep = 1;
                    }
                    else
                    {
                        factorStartIndex = nbOfGaussHermitePoints / 2 - 1;
                        factorEndIndex = 0;
                        factorStep = -1;
                    }
                }
                else
                {
                    if (factorIndex.Value >= nbOfGaussHermitePoints / 2)
                    {
                        factorStartIndex = factorIndex.Value - 1;
                        factorEndIndex = factorIndex.Value - 1;
                    }
                    else
                    {
                        factorStartIndex = 0;
                        factorEndIndex = -1;
                    }
                }

                remainingWeights = 0;

                for (int i = factorStartIndex; i != factorEndIndex + factorStep; i += factorStep)
                {
                    remainingWeights += gaussHermiteWeight[i];
                }

                double[] lossUnitIssuer_2 = new double[betaVector.Length];
                for (int i = 0; i < lossUnitIssuer_2.Length; i++)
                {
                    lossUnitIssuer_2[i] = betaVector[i];
                }

                defaultDistribKnowingFactor = Recursion(numberOfIssuer, defaultProbKnowingFactor, maxRequest, withGreeks);

                for (factorCounter = factorStartIndex; factorCounter != factorEndIndex + factorStep; factorCounter += factorStep)
                {
                    double factor = gaussHermiteAbscissa[factorCounter];
                    double factorWeight = gaussHermiteWeight[factorCounter];

                    for (int issuerCounter = 0; issuerCounter <= forcedMaxRequest; issuerCounter++)
                    {
                        defaultDistrib[issuerCounter, 0] += factorWeight * defaultDistribKnowingFactor[issuerCounter, 0];

                        if (withGreeks)
                        {
                            for (int j = 1; j <= numberOfIssuer; j++)
                            {
                                defaultDistrib[issuerCounter, j] += factorWeight * defaultDistribKnowingFactor[issuerCounter, j] * dp_dProb[j - 1];
                                defaultDistrib[issuerCounter, j + numberOfIssuer] += factorWeight * defaultDistribKnowingFactor[issuerCounter, j] * dp_dBeta[j - 1];
                            }
                        }
                    }

                    remainingWeights -= factorWeight;

                    if ((forcedMaxRequest + 1) * forcedMaxRequest / 2 * remainingWeights < 1e-4)
                    {
                        break;
                    }
                }
            }

            return defaultDistrib;
        }

        public static double[,] RecursionLossUnit(int numberOfIssuer, double[] defaultProbKnowingfFactor, double[] lossUnitIssuer, 
            int[] cumulLossUnitIssuer, int? maxRequest = null, bool withGreeks = false)
        {
            double[,] distrib;
            double p, omp;
            int forcedMaxRequest;
            int jstart;

            if (!maxRequest.HasValue)
            {
                forcedMaxRequest = cumulLossUnitIssuer[numberOfIssuer] - 1;
            }
            else
            {
                forcedMaxRequest = Math.Min(maxRequest.Value, cumulLossUnitIssuer[numberOfIssuer] - 1);
            }

            if (withGreeks)
            {
                distrib = new double[forcedMaxRequest + 2 + (int) UtilityLittleFunctions.MaxTab(lossUnitIssuer, numberOfIssuer), numberOfIssuer + 1];
            }
            else
            {
                distrib = new double[forcedMaxRequest + 2, 1];
            }

            for (int i = 1; i <= forcedMaxRequest + 1; i++)
            {
                distrib[i, 0] = 0;
            }

            distrib[0, 0] = 1;

            for (int issuerCounter = 1; issuerCounter <= numberOfIssuer; issuerCounter++)
            {
                if (lossUnitIssuer[issuerCounter ] > 0)
                {
                    if (cumulLossUnitIssuer[issuerCounter ] > forcedMaxRequest + 1)
                    {
                        jstart = forcedMaxRequest + 1;
                    }
                    else
                    {
                        jstart = cumulLossUnitIssuer[issuerCounter ];
                    }

                    p = defaultProbKnowingfFactor[issuerCounter ];
                    omp = 1 - p;

                    for (int j = jstart; j >= lossUnitIssuer[issuerCounter ]; j--)
                    {
                        distrib[j, 0] = distrib[j, 0] * omp + distrib[j - (int) lossUnitIssuer[issuerCounter ], 0] * p;
                    }

                    int k;
                    if (lossUnitIssuer[issuerCounter] >= jstart)
                    {
                        k = jstart - 1;
                    }
                    else
                    {
                        k = (int)lossUnitIssuer[issuerCounter] - 1;
                    }

                    for (int j = k; j >= 0; j--)
                    {
                        distrib[j, 0] = distrib[j, 0] * omp;
                    }
                }
            }

            if (withGreeks)
            {
                for (int i = 1; i <= numberOfIssuer; i++)
                {
                    if (lossUnitIssuer[i ] > 0)
                    {
                        p = defaultProbKnowingfFactor[i ];
                        omp = 1 - p;
                        double k;
                        if (lossUnitIssuer[i ] - 1 >= forcedMaxRequest)
                        {
                            k = forcedMaxRequest;
                        }
                        else
                        {
                            k = lossUnitIssuer[i]-1;
                        }
                        for (int j = 0; j <= k; j++)
                        {
                            distrib[j, i] = distrib[j, 0] / omp;
                        }

                        for (int j = (int) lossUnitIssuer[i ]; j <= forcedMaxRequest; j++)
                        {
                            distrib[j, i] = (distrib[j, 0] - distrib[j - (int) lossUnitIssuer[i], i] * p) / omp;

                            if (distrib[j, i] < 0)
                            {
                                distrib[j, i] = distrib[j + (int)lossUnitIssuer[i], 0];
                                distrib[j - (int)lossUnitIssuer[i], i] = (distrib[j, 0] - omp * distrib[j, i]) / p;
                            }
                        }

                        for (int j = forcedMaxRequest; j >= lossUnitIssuer[i ]; j--)
                        {
                            distrib[j, i] = distrib[j - (int)lossUnitIssuer[i ], i] - distrib[j, i];
                        }

                        if (lossUnitIssuer[i ] - 1 >= forcedMaxRequest)
                        {
                            k = forcedMaxRequest;         
                        }
                        else
                        {
                            k = lossUnitIssuer[i] - 1;
                        }
                        for (int j = (int)k; j >= 0; j--)
                        {
                            distrib[j, i] = -distrib[j, i];
                        }
                    }
                }
            }

            return distrib;
        }
        public static double[,] RecursionLossUnitORIGINAL(int numberOfIssuer, double[] defaultProbKnowingfFactor, double[] lossUnitIssuer,
            int[] cumulLossUnitIssuer, int? maxRequest = null, bool withGreeks = false)
        {
            double[,] distrib;
            double p, omp;
            int forcedMaxRequest;
            int jstart;

            if (!maxRequest.HasValue)
            {
                forcedMaxRequest = cumulLossUnitIssuer[numberOfIssuer - 1] - 1;
            }
            else
            {
                forcedMaxRequest = Math.Min(maxRequest.Value, cumulLossUnitIssuer[numberOfIssuer - 1] - 1);
            }

            if (withGreeks)
            {
                distrib = new double[forcedMaxRequest + 2 + (int)UtilityLittleFunctions.MaxTab(lossUnitIssuer, numberOfIssuer), numberOfIssuer + 1];
            }
            else
            {
                distrib = new double[forcedMaxRequest + 2, 1];
            }

            for (int i = 1; i <= forcedMaxRequest + 1; i++)
            {
                distrib[i, 0] = 0;
            }

            distrib[0, 0] = 1;

            for (int issuerCounter = 1; issuerCounter <= numberOfIssuer; issuerCounter++)
            {
                if (lossUnitIssuer[issuerCounter - 1] > 0)
                {
                    if (cumulLossUnitIssuer[issuerCounter - 1] > forcedMaxRequest + 1)
                    {
                        jstart = forcedMaxRequest + 1;
                    }
                    else
                    {
                        jstart = cumulLossUnitIssuer[issuerCounter - 1];
                    }

                    p = defaultProbKnowingfFactor[issuerCounter - 1];
                    omp = 1 - p;

                    for (int j = jstart; j >= lossUnitIssuer[issuerCounter - 1]; j--)
                    {
                        distrib[j, 0] = distrib[j, 0] * omp + distrib[j - (int)lossUnitIssuer[issuerCounter - 1], 0] * p;
                    }

                    int k;
                    if (lossUnitIssuer[issuerCounter - 1] >= jstart)
                    {
                        k = jstart - 1;
                    }
                    else
                    {
                        k = (int)lossUnitIssuer[issuerCounter - 1] - 1;
                    }

                    for (int j = k; j >= 0; j--)
                    {
                        distrib[j, 0] = distrib[j, 0] * omp;
                    }
                }
            }

            if (withGreeks)
            {
                for (int i = 1; i <= numberOfIssuer; i++)
                {
                    if (lossUnitIssuer[i - 1] > 0)
                    {
                        p = defaultProbKnowingfFactor[i - 1];
                        omp = 1 - p;

                        if (lossUnitIssuer[i - 1] - 1 >= forcedMaxRequest)
                        {
                            int k = forcedMaxRequest;

                            for (int j = 0; j <= k; j++)
                            {
                                distrib[j, i] = distrib[j, 0] / omp;
                            }
                        }

                        int kLossUnitIssuer;
                        if (lossUnitIssuer[i - 1] - 1 >= forcedMaxRequest)
                        {
                            kLossUnitIssuer = forcedMaxRequest;
                        }
                        else
                        {
                            kLossUnitIssuer = (int)lossUnitIssuer[i - 1] - 1;
                        }

                        for (int j = (int)lossUnitIssuer[i - 1]; j <= forcedMaxRequest; j++)
                        {
                            distrib[j, i] = (distrib[j, 0] - distrib[j - (int)lossUnitIssuer[i - 1], i] * p) / omp;

                            if (distrib[j, i] < 0)
                            {
                                distrib[j, i] = distrib[j + (int)lossUnitIssuer[i - 1], 0];
                                distrib[j - (int)lossUnitIssuer[i - 1], i] = (distrib[j, 0] - omp * distrib[j, i]) / p;
                            }
                        }

                        for (int j = forcedMaxRequest; j >= lossUnitIssuer[i - 1]; j--)
                        {
                            distrib[j, i] = distrib[j - (int)lossUnitIssuer[i - 1], i] - distrib[j, i];
                        }

                        if (lossUnitIssuer[i - 1] - 1 >= forcedMaxRequest)
                        {
                            int k = forcedMaxRequest;

                            for (int j = k; j >= 0; j--)
                            {
                                distrib[j, i] = -distrib[j, i];
                            }
                        }
                    }
                }
            }

            return distrib;
        }

        public static double[,] GetDefaultDistributionLossUnit(int numberOfIssuer, double[] defaultProbability, int[] lossUnitIssuer, 
            int[] cumulLossUnitIssuer, double[] betaVector, int? maxRequest = null, 
            double[] inputThreshold = null, int? factorIndex = null, bool withGreeks = false, double dBeta =0.1)
        {
            double[,] defaultDistribKnowingFactor;
            double[] defaultDistrib;
            double[] defaultThreshold = new double[numberOfIssuer];
            double[] defaultProbKnowingFactor = new double[numberOfIssuer+1];//MODIF original new double[numberOfIssuer];
            double[] dp_dProb = new double[numberOfIssuer];
            double[] dp_dBeta = new double[numberOfIssuer];
            double[] dDefaultThreshold = new double[numberOfIssuer];
            double factor;
            double factorWeight;
            double remainingWeights;
            int forcedMaxRequest;
            int factorCounter;
            int nbOfGaussHermitePoints = 64;
            //int gaussHermiteAbscissaLength = 64;
            double gaussHermiteMidTable = nbOfGaussHermitePoints / 2 + 1;
            double[] gaussHermiteAbscissa = new double[]
            {
                -10.5261231679605, -9.89528758682953, -9.37315954964672, -8.90724909996476, -8.47752908337986, -8.07368728501022, -7.68954016404049, -7.32101303278094,
                -6.9652411205511, -6.62011226263602, -6.28401122877482, -5.95566632679948, -5.63405216434997, -5.31832522463327, -5.00777960219876, -4.70181564740749,
                -4.39991716822813, -4.10163447456665, -3.80657151394536, -3.5143759357409, -3.22473129199203, -2.93735082300462, -2.65197243543063, -2.3683545886324,
                -2.08627287988176, -1.80551717146554, -1.52588914020986, -1.24720015694311, -0.969269423071178, -0.691922305810044, -0.414988824121078, -0.138302244987009,
                 0.138302244987009, 0.414988824121078, 0.691922305810044, 0.969269423071178, 1.24720015694311, 1.52588914020986, 1.80551717146554, 2.08627287988176,
                 2.3683545886324, 2.65197243543063, 2.93735082300462, 3.22473129199203, 3.5143759357409, 3.80657151394536, 4.10163447456665, 4.39991716822813,
                 4.70181564740749, 5.00777960219876, 5.31832522463327, 5.63405216434997, 5.95566632679948, 6.28401122877482, 6.62011226263602, 6.9652411205511,
                 7.32101303278094, 7.68954016404049, 8.07368728501022, 8.47752908337986, 8.90724909996476, 9.37315954964672, 9.89528758682953, 10.5261231679605
            };
            double[] gaussHermiteWeight = new double[]
            {
                2.53418710060459E-25, 1.22602411040096E-22, 1.63228017722665E-20, 1.05092412334527E-18, 4.10618806999281E-17, 1.09871222523276E-15, 2.16838907840016E-14, 3.31758747989528E-13,
                4.07710009958856E-12, 4.13239336761409E-11, 3.5254084284956E-10, 2.57250350487446E-09, 1.62656661863386E-08, 9.00688368649578E-08, 4.40658777599536E-07, 1.91902972761357E-06,
                7.48598005351949E-06, 2.62991006123899E-05, 8.3592377852976E-05, 2.41356276405779E-04, 6.35207286288721E-04, 1.52839476722543E-03, 3.37087743793626E-03, 6.82981749780981E-03,
                1.27370763383524E-02, 2.18997473380829E-02, 3.47633024351237E-02, 5.10056393070224E-02, 6.92371821856065E-02, 8.70172186157688E-02, 0.101310028539473, 0.109304305522575,
                0.109304305522575, 0.101310028539473, 8.70172186157688E-02, 6.92371821856065E-02, 5.10056393070224E-02, 3.47633024351237E-02, 2.18997473380829E-02, 1.27370763383524E-02,
                6.82981749780981E-03, 3.37087743793626E-03, 1.52839476722543E-03, 6.35207286288721E-04, 2.41356276405779E-04, 8.3592377852976E-05, 2.62991006123899E-05, 7.48598005351949E-06,
                1.91902972761357E-06, 4.40658777599536E-07, 9.00688368649578E-08, 1.62656661863386E-08, 2.57250350487446E-09, 3.5254084284956E-10, 4.13239336761409E-11, 4.07710009958856E-12,
                3.31758747989528E-13, 2.16838907840016E-14, 1.09871222523276E-15, 4.10618806999281E-17, 1.05092412334527E-18, 1.63228017722665E-20, 1.22602411040096E-22, 2.53418710060459E-25
            };

            if (!maxRequest.HasValue)
            {
                forcedMaxRequest = cumulLossUnitIssuer[numberOfIssuer ] - 1;
            }
            else
            {
                forcedMaxRequest = Math.Min(maxRequest.Value, cumulLossUnitIssuer[numberOfIssuer ] - 1);
            }

            if (inputThreshold == null)
            {
                for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                {
                    if (defaultProbability[issuerCounter] == 0.0)
                    {
                        defaultThreshold[issuerCounter] = -100.0;
                    }
                    else
                    {
                        defaultThreshold[issuerCounter] = Normal.InvCDF(0.0, 1.0, defaultProbability[issuerCounter]);
                    }
                }

                if (withGreeks)
                {
                    for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                    {
                        if (defaultProbability[issuerCounter] * ShockMultiplier >= 1.0)//MODIF, original pas de shockmultiplier ?
                        {
                            Console.WriteLine($"Can't shock default probability of issuer # {issuerCounter + 1}!");
                            Console.WriteLine("Delta will be wrong");
                            dDefaultThreshold[issuerCounter] = defaultThreshold[issuerCounter];
                        }
                        else
                        {
                            if (defaultProbability[issuerCounter] == 0.0)
                            {
                                dDefaultThreshold[issuerCounter] = Normal.InvCDF(0.0, 1.0, 0.0001);
                            }
                            else
                            {
                                dDefaultThreshold[issuerCounter] = Normal.InvCDF(0.0, 1.0, defaultProbability[issuerCounter] *ShockMultiplier);//MODIF, original pas de shockmultiplier ?
                            }
                        }
                    }
                }
            }
            else
            {
                defaultThreshold = inputThreshold;
            }

            double[,] defaultDistribArray;
            if (!withGreeks)
            {
                defaultDistribArray = new double[forcedMaxRequest + 2, 1];
            }
            else
            {
                defaultDistribArray = new double[forcedMaxRequest + 2, 2 * numberOfIssuer + 1];
            }

            for (int lossUnitCounter = 0; lossUnitCounter <= forcedMaxRequest + 1; lossUnitCounter++)
            {
                defaultDistribArray[lossUnitCounter, 0] = 0.0;
                if (withGreeks)
                {
                    for (int j = 1; j <= numberOfIssuer; j++)
                    {
                        defaultDistribArray[lossUnitCounter, j] = 0.0;
                        defaultDistribArray[lossUnitCounter, j + numberOfIssuer] = 0.0;
                    }
                }
            }

            for (int piece = 1; piece <= 2; piece++)
            {
                int factorStartIndex;
                int factorEndIndex;
                int factorStep = -1;
                if (!factorIndex.HasValue)
                {
                    if (piece == 1)
                    {
                        factorStartIndex = (int)gaussHermiteMidTable - 1;
                        factorEndIndex = nbOfGaussHermitePoints - 1;
                        factorStep = 1;
                    }
                    else
                    {
                        factorStartIndex = (int)gaussHermiteMidTable - 2;
                        factorEndIndex = 0;
                        factorStep = -1;
                    }
                }
                else
                {
                    if (factorIndex >= gaussHermiteMidTable)
                    {
                        factorStartIndex = (int)factorIndex - 1;
                        factorEndIndex = (int)factorIndex - 1;
                    }
                    else
                    {
                        factorStartIndex = 0;
                        factorEndIndex = -1;
                    }
                }

                remainingWeights = 0.0;
                for (factorCounter = factorStartIndex; factorCounter != factorEndIndex + Math.Sign(factorEndIndex - factorStartIndex); factorCounter += factorStep)// Permet de rentrer dans la boucle quand factorstep = -1
                {
                    remainingWeights += gaussHermiteWeight[factorCounter];
                }

                for (factorCounter = factorStartIndex; factorCounter != factorEndIndex+Math.Sign(factorEndIndex-factorStartIndex); factorCounter += factorStep)
                {
                    factor = gaussHermiteAbscissa[factorCounter];
                    factorWeight = gaussHermiteWeight[factorCounter];

                    for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                    {
                        double beta = betaVector[issuerCounter];
                        defaultProbKnowingFactor[issuerCounter+1] = UtilityBiNormal.NormalCumulativeDistribution((defaultThreshold[issuerCounter] - beta * factor) / Math.Sqrt(1.0 - beta * beta));//MODIF original defaultProbKnowingFactor[issuerCounter]
                    }

                    if (withGreeks)
                    {
                        for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                        {
                            double beta = betaVector[issuerCounter];
                            dp_dProb[issuerCounter] = UtilityBiNormal.NormalCumulativeDistribution((dDefaultThreshold[issuerCounter] - beta * factor) / Math.Sqrt(1.0 - beta * beta)) - defaultProbKnowingFactor[issuerCounter+1];//Modif IDEM
                            beta += dBeta;
                            dp_dBeta[issuerCounter] = UtilityBiNormal.NormalCumulativeDistribution((defaultThreshold[issuerCounter] - beta * factor) / Math.Sqrt(1.0 - beta * beta)) - defaultProbKnowingFactor[issuerCounter+1];// Modif IDEM
                        }
                    }

                    double[] lossUnitIssuer_2 = new double[lossUnitIssuer.Length];
                    for(int i = 0;i < lossUnitIssuer_2.Length; i++)
                    {
                        lossUnitIssuer_2[i] = lossUnitIssuer[i];
                    }

                    defaultDistribKnowingFactor = RecursionLossUnit(numberOfIssuer, defaultProbKnowingFactor,
                        lossUnitIssuer_2, cumulLossUnitIssuer, maxRequest, withGreeks);

                    //int numberOfIssuer, double[] defaultProbKnowingfFactor, int[] lossUnitIssuer, int[] cumulLossUnitIssuer, 
                      //  int? maxRequest = null, bool withGreeks = false

                    for (int lossUnitCounter = 0; lossUnitCounter <= forcedMaxRequest + 1; lossUnitCounter++)
                    {
                        defaultDistribArray[lossUnitCounter, 0] += factorWeight * defaultDistribKnowingFactor[lossUnitCounter, 0];
                        if (withGreeks)
                        {
                            for (int j = 0; j < numberOfIssuer; j++)
                            {
                                defaultDistribArray[lossUnitCounter, j+1] += factorWeight * defaultDistribKnowingFactor[lossUnitCounter, j+1] * dp_dProb[j];
                                defaultDistribArray[lossUnitCounter, j + numberOfIssuer+1] += factorWeight * defaultDistribKnowingFactor[lossUnitCounter, j+1] * dp_dBeta[j];
                            }
                        }
                    }

                    remainingWeights -= factorWeight;

                    if ((forcedMaxRequest + 1) * forcedMaxRequest / 2.0 * remainingWeights < 1.0 * 0.0001)
                    {
                        break;
                    }
                }
            }

            return defaultDistribArray;
        }
        public static double[,] GetDefaultDistributionLossUnitORIGINAL(int numberOfIssuer, double[] defaultProbability, int[] lossUnitIssuer,
    int[] cumulLossUnitIssuer, double[] betaVector, int? maxRequest = null,
    double[] inputThreshold = null, int? factorIndex = null, bool withGreeks = false, double dBeta = 0.1)
        {
            double[,] defaultDistribKnowingFactor;
            double[] defaultDistrib;
            double[] defaultThreshold = new double[numberOfIssuer];
            double[] defaultProbKnowingFactor = new double[numberOfIssuer];
            double[] dp_dProb = new double[numberOfIssuer];
            double[] dp_dBeta = new double[numberOfIssuer];
            double[] dDefaultThreshold = new double[numberOfIssuer];
            double factor;
            double factorWeight;
            double remainingWeights;
            int forcedMaxRequest;
            int factorCounter;
            int nbOfGaussHermitePoints = 64;
            //int gaussHermiteAbscissaLength = 64;
            double gaussHermiteMidTable = nbOfGaussHermitePoints / 2 + 1;
            double[] gaussHermiteAbscissa = new double[]
            {
                -10.5261231679605, -9.89528758682953, -9.37315954964672, -8.90724909996476, -8.47752908337986, -8.07368728501022, -7.68954016404049, -7.32101303278094,
                -6.9652411205511, -6.62011226263602, -6.28401122877482, -5.95566632679948, -5.63405216434997, -5.31832522463327, -5.00777960219876, -4.70181564740749,
                -4.39991716822813, -4.10163447456665, -3.80657151394536, -3.5143759357409, -3.22473129199203, -2.93735082300462, -2.65197243543063, -2.3683545886324,
                -2.08627287988176, -1.80551717146554, -1.52588914020986, -1.24720015694311, -0.969269423071178, -0.691922305810044, -0.414988824121078, -0.138302244987009,
                 0.138302244987009, 0.414988824121078, 0.691922305810044, 0.969269423071178, 1.24720015694311, 1.52588914020986, 1.80551717146554, 2.08627287988176,
                 2.3683545886324, 2.65197243543063, 2.93735082300462, 3.22473129199203, 3.5143759357409, 3.80657151394536, 4.10163447456665, 4.39991716822813,
                 4.70181564740749, 5.00777960219876, 5.31832522463327, 5.63405216434997, 5.95566632679948, 6.28401122877482, 6.62011226263602, 6.9652411205511,
                 7.32101303278094, 7.68954016404049, 8.07368728501022, 8.47752908337986, 8.90724909996476, 9.37315954964672, 9.89528758682953, 10.5261231679605
            };
            double[] gaussHermiteWeight = new double[]
            {
                2.53418710060459E-25, 1.22602411040096E-22, 1.63228017722665E-20, 1.05092412334527E-18, 4.10618806999281E-17, 1.09871222523276E-15, 2.16838907840016E-14, 3.31758747989528E-13,
                4.07710009958856E-12, 4.13239336761409E-11, 3.5254084284956E-10, 2.57250350487446E-09, 1.62656661863386E-08, 9.00688368649578E-08, 4.40658777599536E-07, 1.91902972761357E-06,
                7.48598005351949E-06, 2.62991006123899E-05, 8.3592377852976E-05, 2.41356276405779E-04, 6.35207286288721E-04, 1.52839476722543E-03, 3.37087743793626E-03, 6.82981749780981E-03,
                1.27370763383524E-02, 2.18997473380829E-02, 3.47633024351237E-02, 5.10056393070224E-02, 6.92371821856065E-02, 8.70172186157688E-02, 0.101310028539473, 0.109304305522575,
                0.109304305522575, 0.101310028539473, 8.70172186157688E-02, 6.92371821856065E-02, 5.10056393070224E-02, 3.47633024351237E-02, 2.18997473380829E-02, 1.27370763383524E-02,
                6.82981749780981E-03, 3.37087743793626E-03, 1.52839476722543E-03, 6.35207286288721E-04, 2.41356276405779E-04, 8.3592377852976E-05, 2.62991006123899E-05, 7.48598005351949E-06,
                1.91902972761357E-06, 4.40658777599536E-07, 9.00688368649578E-08, 1.62656661863386E-08, 2.57250350487446E-09, 3.5254084284956E-10, 4.13239336761409E-11, 4.07710009958856E-12,
                3.31758747989528E-13, 2.16838907840016E-14, 1.09871222523276E-15, 4.10618806999281E-17, 1.05092412334527E-18, 1.63228017722665E-20, 1.22602411040096E-22, 2.53418710060459E-25
            };

            if (!maxRequest.HasValue)
            {
                forcedMaxRequest = cumulLossUnitIssuer[numberOfIssuer - 1] - 1;
            }
            else
            {
                forcedMaxRequest = Math.Min(maxRequest.Value, cumulLossUnitIssuer[numberOfIssuer - 1] - 1);
            }

            if (inputThreshold == null)
            {
                for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                {
                    if (defaultProbability[issuerCounter] == 0.0)
                    {
                        defaultThreshold[issuerCounter] = -100.0;
                    }
                    else
                    {
                        defaultThreshold[issuerCounter] = Normal.InvCDF(0.0, 1.0, defaultProbability[issuerCounter]);
                    }
                }

                if (withGreeks)
                {
                    for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                    {
                        if (defaultProbability[issuerCounter] * 1.0 >= 1.0)
                        {
                            Console.WriteLine($"Can't shock default probability of issuer # {issuerCounter + 1}!");
                            Console.WriteLine("Delta will be wrong");
                            dDefaultThreshold[issuerCounter] = defaultThreshold[issuerCounter];
                        }
                        else
                        {
                            if (defaultProbability[issuerCounter] == 0.0)
                            {
                                dDefaultThreshold[issuerCounter] = Normal.InvCDF(0.0, 1.0, 0.0001);
                            }
                            else
                            {
                                dDefaultThreshold[issuerCounter] = Normal.InvCDF(0.0, 1.0, defaultProbability[issuerCounter] * 1.0);
                            }
                        }
                    }
                }
            }
            else
            {
                defaultThreshold = inputThreshold;
            }

            double[,] defaultDistribArray;
            if (!withGreeks)
            {
                defaultDistribArray = new double[forcedMaxRequest + 2, 1];
            }
            else
            {
                defaultDistribArray = new double[forcedMaxRequest + 2, 2 * numberOfIssuer + 1];
            }

            for (int lossUnitCounter = 0; lossUnitCounter <= forcedMaxRequest + 1; lossUnitCounter++)
            {
                defaultDistribArray[lossUnitCounter, 0] = 0.0;
                if (withGreeks)
                {
                    for (int j = 1; j <= numberOfIssuer; j++)
                    {
                        defaultDistribArray[lossUnitCounter, j] = 0.0;
                        defaultDistribArray[lossUnitCounter, j + numberOfIssuer] = 0.0;
                    }
                }
            }

            for (int piece = 1; piece <= 2; piece++)
            {
                int factorStartIndex;
                int factorEndIndex;
                int factorStep = -1;
                if (!factorIndex.HasValue)
                {
                    if (piece == 1)
                    {
                        factorStartIndex = (int)gaussHermiteMidTable - 1;
                        factorEndIndex = nbOfGaussHermitePoints - 1;
                        factorStep = 1;
                    }
                    else
                    {
                        factorStartIndex = (int)gaussHermiteMidTable - 2;
                        factorEndIndex = 0;
                        factorStep = -1;
                    }
                }
                else
                {
                    if (factorIndex >= gaussHermiteMidTable)
                    {
                        factorStartIndex = (int)factorIndex - 1;
                        factorEndIndex = (int)factorIndex - 1;
                    }
                    else
                    {
                        factorStartIndex = 0;
                        factorEndIndex = -1;
                    }
                }

                remainingWeights = 0.0;
                for (factorCounter = factorStartIndex; factorCounter != factorEndIndex + Math.Sign(factorEndIndex - factorStartIndex); factorCounter += factorStep)// Permet de rentrer dans la boucle quand factorstep = -1
                {
                    remainingWeights += gaussHermiteWeight[factorCounter];
                }

                for (factorCounter = factorStartIndex; factorCounter != factorEndIndex + Math.Sign(factorEndIndex - factorStartIndex); factorCounter += factorStep)
                {
                    factor = gaussHermiteAbscissa[factorCounter];
                    factorWeight = gaussHermiteWeight[factorCounter];

                    for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                    {
                        double beta = betaVector[issuerCounter];
                        defaultProbKnowingFactor[issuerCounter] = UtilityBiNormal.NormalCumulativeDistribution((defaultThreshold[issuerCounter] - beta * factor) / Math.Sqrt(1.0 - beta * beta));
                    }

                    if (withGreeks)
                    {
                        for (int issuerCounter = 0; issuerCounter < numberOfIssuer; issuerCounter++)
                        {
                            double beta = betaVector[issuerCounter];
                            dp_dProb[issuerCounter] = UtilityBiNormal.NormalCumulativeDistribution((dDefaultThreshold[issuerCounter] - beta * factor) / Math.Sqrt(1.0 - beta * beta)) - defaultProbKnowingFactor[issuerCounter];
                            beta += dBeta;
                            dp_dBeta[issuerCounter] = UtilityBiNormal.NormalCumulativeDistribution((defaultThreshold[issuerCounter] - beta * factor) / Math.Sqrt(1.0 - beta * beta)) - defaultProbKnowingFactor[issuerCounter];
                        }
                    }

                    double[] lossUnitIssuer_2 = new double[lossUnitIssuer.Length];
                    for (int i = 0; i < lossUnitIssuer_2.Length; i++)
                    {
                        lossUnitIssuer_2[i] = lossUnitIssuer[i];
                    }

                    defaultDistribKnowingFactor = RecursionLossUnit(numberOfIssuer, defaultProbKnowingFactor,
                        lossUnitIssuer_2, cumulLossUnitIssuer, maxRequest, withGreeks);

                    //int numberOfIssuer, double[] defaultProbKnowingfFactor, int[] lossUnitIssuer, int[] cumulLossUnitIssuer, 
                    //  int? maxRequest = null, bool withGreeks = false

                    for (int lossUnitCounter = 0; lossUnitCounter <= forcedMaxRequest + 1; lossUnitCounter++)
                    {
                        defaultDistribArray[lossUnitCounter, 0] += factorWeight * defaultDistribKnowingFactor[lossUnitCounter, 0];
                        if (withGreeks)
                        {
                            for (int j = 0; j < numberOfIssuer; j++)
                            {
                                defaultDistribArray[lossUnitCounter, j + 1] += factorWeight * defaultDistribKnowingFactor[lossUnitCounter, j] * dp_dProb[j];
                                defaultDistribArray[lossUnitCounter, j + numberOfIssuer + 1] += factorWeight * defaultDistribKnowingFactor[lossUnitCounter, j] * dp_dBeta[j];
                            }
                        }
                    }

                    remainingWeights -= factorWeight;

                    if ((forcedMaxRequest + 1) * forcedMaxRequest / 2.0 * remainingWeights < 1.0 * 0.0001)
                    {
                        break;
                    }
                }
            }

            return defaultDistribArray;
        }
        public static object[,] EuropeanCDOLossUnit(int numberOfIssuer, double lossUnitAmount, double[] strikes, double[] defaultProbability,
        double correl, double[] betaAdder, double zC, double[] nominalIssuer, double[] recoveryIssuer, bool withGreeks = false, double dBeta = 0.1)
        {
            int r, c;
            object[,] res;

            if (withGreeks)
            {
                r = 1 + 2 * numberOfIssuer;
            }
            else { r = 1; }
            c = strikes.Length + 1;
            res = new object[r, c];

            for (int i = 0; i < r; i++)
            {
                for (int j = 0; j < c; j++)
                {
                    res[i, j] = 0.0;
                }
            }

            res[0, 0] = "PV";

            if (withGreeks)
            {
                for (int j = 1; j <= strikes.Length; j++)
                {
                    res[0, j] = 0.0;//MODIF original : res[1, j] = 0.0;
                }

                for (int i = 1; i <= numberOfIssuer * 2; i++)
                {
                    if (i <= numberOfIssuer)
                    {
                        res[i, 0] = "dpv prob " + i;//MODIF original : res[i,0]
                    }
                    else
                    {
                        res[i, 0] = "dpv beta " + (i - numberOfIssuer);//MODIF original : res[i,0]
                    }

                    for (int j = 1; j <= strikes.Length; j++)
                    {
                        res[i , j] = 0.0; //MODIF original : res[i,0]

                    }
                }
            }

            else
            {
                for (int j = 1; j <= strikes.Length; j++)
                {
                    res[0, j] = 0;
                }
            }

            int[] lossUnitIssuer = new int[numberOfIssuer+1];
            int[] cumulLossUnitIssuer = new int[numberOfIssuer+1];

            lossUnitIssuer[0] = 0;
            cumulLossUnitIssuer[0] = 0;

            for (int issuerCounter = 1; issuerCounter <= numberOfIssuer; issuerCounter++)
            {
                lossUnitIssuer[issuerCounter] = (int)Math.Round((nominalIssuer[issuerCounter-1] * (1 - recoveryIssuer[issuerCounter-1])) / lossUnitAmount);
                
            }
            //Console.WriteLine("iici 12");

            cumulLossUnitIssuer[1] = lossUnitIssuer[1];

            for (int issuerCounter = 2; issuerCounter <= numberOfIssuer; issuerCounter++)
            {               
                cumulLossUnitIssuer[issuerCounter] = lossUnitIssuer[issuerCounter] + cumulLossUnitIssuer[issuerCounter-1];
            }


            int sumLossUnit = cumulLossUnitIssuer[numberOfIssuer];

            int maxNumLossUnitToReachStrikes = (int)(strikes.Max() / lossUnitAmount)+1;

            if (maxNumLossUnitToReachStrikes > sumLossUnit -1)
            {            
                maxNumLossUnitToReachStrikes = sumLossUnit - 1;
            }

            double[] betaVector = new double[numberOfIssuer];
            double sqrtCorrel = Math.Sqrt(correl);

            for (int i = 0; i < numberOfIssuer; i++)
            {
                betaVector[i] = sqrtCorrel + betaAdder[i];
            }

            //Console.WriteLine("hvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv");

            //Console.WriteLine("lossUnitIssuer0=" + lossUnitIssuer[0]);
            //Console.WriteLine("lossUnitIssuer1=" + lossUnitIssuer[1]);
            //Console.WriteLine("lossUnitIssuer2=" + lossUnitIssuer[2]);
            //Console.WriteLine("lossUnitIssuer.lengh" + lossUnitIssuer.Length);
            //Console.WriteLine("cumulLossUnitIssue0r=" + cumulLossUnitIssuer[0]);
            //Console.WriteLine("cumulLossUnitIssuer1=" + cumulLossUnitIssuer[1]);
            //Console.WriteLine("cumulLossUnitIssuer2=" + cumulLossUnitIssuer[2]);
            //Console.WriteLine("cumulLossUnitIssuerlenng=" + cumulLossUnitIssuer.Length);
            //Console.WriteLine("betaVector0=" + betaVector[0]);
            //Console.WriteLine("betaVector1=" + betaVector[1]);
            //Console.WriteLine("betaVector2=" + betaVector[2]);
            //Console.WriteLine("betaVectorlengh=" + betaVector.Length);

            double[,] defaultDistribution = GetDefaultDistributionLossUnit(numberOfIssuer, defaultProbability, lossUnitIssuer, 
                cumulLossUnitIssuer, betaVector, maxNumLossUnitToReachStrikes, null, null, withGreeks, dBeta);

            for (int strikeCounter = 1; strikeCounter <= strikes.Length; strikeCounter++)
            {                              
                double strike = strikes[strikeCounter - 1] / lossUnitAmount;
                maxNumLossUnitToReachStrikes = (int)strike;

                if (maxNumLossUnitToReachStrikes > sumLossUnit - 1)
                {
                    maxNumLossUnitToReachStrikes = sumLossUnit - 1;
                }

                double calcPV = 0.0;
                double residualProb = 1.0 - defaultDistribution[0, 0];
               
                for (int lossUnitCounter = 1; lossUnitCounter <= maxNumLossUnitToReachStrikes; lossUnitCounter++)
                {
                    calcPV += lossUnitCounter * defaultDistribution[lossUnitCounter, 0];
                    residualProb -= defaultDistribution[lossUnitCounter, 0];
                }

                calcPV += UtilityLittleFunctions.MinOf(strike, sumLossUnit) * residualProb;

                res[0, strikeCounter] = calcPV * zC * lossUnitAmount;

                if (withGreeks)
                {
                    for (int i = 0; i < numberOfIssuer; i++)
                    {
                        calcPV = 0.0;
                        residualProb = 0.0 - defaultDistribution[0, i+1];//MODIF, original : defaultDistribution[0, i]

                        for (int lossUnitCounter = 1; lossUnitCounter <= maxNumLossUnitToReachStrikes; lossUnitCounter++)
                        {
                            calcPV += lossUnitCounter * defaultDistribution[lossUnitCounter, i+1];//MODIF, original : defaultDistribution[lossUnitCounter, i]
                            residualProb -= defaultDistribution[lossUnitCounter, i+1];//MODIF, IDEM
                        }

                        calcPV += UtilityLittleFunctions.MinOf(strike, sumLossUnit) * residualProb;

                        if (defaultProbability[i] == 0.0)
                        {
                            res[ i+1, strikeCounter] = calcPV * zC * lossUnitAmount / (0.0001);
                        }
                        else
                        {
                            res[ i+1, strikeCounter] = calcPV * zC * lossUnitAmount / (defaultProbability[i] * 0.05);
                        }

                        calcPV = 0.0;
                        residualProb = 0.0 - defaultDistribution[0, i + numberOfIssuer+1];//MODIF, original : defaultDistribution[0, i + numberOfIssuer]
                        for (int lossUnitCounter = 1; lossUnitCounter <= maxNumLossUnitToReachStrikes; lossUnitCounter++)
                        {
                            calcPV += lossUnitCounter * defaultDistribution[lossUnitCounter, i + numberOfIssuer+1];//IDEM
                            residualProb -= defaultDistribution[lossUnitCounter, i + numberOfIssuer+1];//IDEM
                        }

                        calcPV += UtilityLittleFunctions.MinOf(strike, sumLossUnit) * residualProb;
                        res[1 + i + numberOfIssuer, strikeCounter] = calcPV * zC * lossUnitAmount;
                    }
                }
            }

            return res;
        }
    }
}
