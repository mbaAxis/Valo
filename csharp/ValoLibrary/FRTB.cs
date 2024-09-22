using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoLibrary
{
    public class FRTB
    {
        public static double[] DeltaGIRR(int lastIndice, string girrCurrency, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            double additionalWeight = 1;
            if (pricingCurrency == "EUR" || pricingCurrency == "USD" || pricingCurrency == "GPB" || pricingCurrency == "AUD" || pricingCurrency == "JPY" || pricingCurrency == "SEK" || pricingCurrency == "CAD")//see 21.44
            {
                additionalWeight = Math.Sqrt(2);
            }
            double shockedGIRR = 0.0001;
            string[] tenors = { "3M", "6M", "1Y", "2Y", "3Y", "5Y", "10Y", "15Y", "20Y", "30Y" };
            int[] months = { 3, 6, 12, 24, 36, 60, 120, 180, 240, 360 };
            double[] riskWeights = { 0.017, 0.017, 0.016, 0.013, 0.012, 0.011, 0.011, 0.011, 0.011, 0.011 };
            double[] CDOresults = new double[tenors.Length];
            double[,] CDSresults = new double[lastIndice, tenors.Length];
            double[] nonShocked = new double[10];
            double[] shocked = new double[10];
            double[] results = new double[tenors.Length];
            for (int i = 0; i < tenors.Length; i++)
            {
                for (int j = 0; j < lastIndice; j++)
                {
                    CDSresults[j, i] = -Double.Parse(ModelInterface.CDS(issuerList[j], maturity, spreadStandard[j] * shockedGIRR, recoveryIssuer[j], nominalIssuer[j], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1, 0, hedgingCDS, integrationPeriod, 1, 0, girrCurrency)[0, 0]);
                    CDSresults[j, i] += Double.Parse(ModelInterface.CDS(issuerList[j], maturity, spreadStandard[j] * shockedGIRR, recoveryIssuer[j], nominalIssuer[j], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1, 0, hedgingCDS, integrationPeriod, 1, months[i], girrCurrency)[0, 0]);
                    CDSresults[j, i] /= shockedGIRR;
                    CDSresults[j, i] *= riskWeights[i] / additionalWeight;
                    results[i] -= CDSresults[j, i];
                }
                nonShocked[i] = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod,
                    cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0,
                    0, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta, 0, girrCurrency)[0, 0]);
                shocked[i] = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod,
                    cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0,
                    0, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta, months[i], girrCurrency)[0, 0]);
                CDOresults[i] = (shocked[i] - nonShocked[i]) / shockedGIRR;
                CDOresults[i] *= riskWeights[i] / additionalWeight;
                results[i] += CDOresults[i];
            }
            return results;
        }
        public static double[,] Girr(DateTime paramDate, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            int indiceIssuer = issuerList.Length;
            for (int i = 0; i < issuerList.Length; i++)//In case there is less issuer than given
            {
                if (issuerList[i] == "" || String.IsNullOrEmpty(issuerList[i]))
                {
                    indiceIssuer = i;
                    break;
                }
            }
            //Computation of all sensitivities with respect to all curves stored
            StrippingIRS.IRCurveStore[] curvesList = StrippingIRS.CurveList;
            int lastIndice = 0;
            for (int i = 1; i < curvesList.Length; i++)
            {
                if (!String.Equals(curvesList[i].Currency, curvesList[i - 1].Currency))
                {
                    lastIndice++;//number of bucket
                }
            }
            int[] indice = new int[lastIndice + 1];
            indice[0] = 0;
            lastIndice = 0;
            for (int i = 1; i < curvesList.Length; i++)
            {
                if (!String.Equals(curvesList[i].Currency, curvesList[i - 1].Currency))
                {
                    lastIndice++;
                    indice[lastIndice] = i;
                }
            }
            double[,] girrSensitivities = new double[curvesList.Length, 11];
            for (int i = 1; i < indice.Length; i++)
            {
                StrippingIRS.StripZC(paramDate, curvesList[indice[i]].Currency, curvesList[indice[i]].SwapRates, curvesList[indice[i]].CurveDates,
                    curvesList[indice[i]].SwapPeriod, curvesList[indice[i]].SwapBasis, curvesList[indice[i]].FXRate);//Strip toutes les autres courbes autre que la première currency
            }
            for (int i = 0; i < curvesList.Length; i++)
            {
                StrippingIRS.StripZC(paramDate, curvesList[i].Currency, curvesList[i].SwapRates, curvesList[i].CurveDates,
                    curvesList[i].SwapPeriod, curvesList[i].SwapBasis, curvesList[i].FXRate);
                for (int j = 1; j < indice.Length; j++)//on considère juste que les premières courbes de chaque currency sont celle de base qu'on veut tjrs
                {
                    if (i == indice[j])
                    {
                        StrippingIRS.StripZC(paramDate, curvesList[indice[j - 1]].Currency, curvesList[indice[j - 1]].SwapRates, curvesList[indice[j - 1]].CurveDates,
                    curvesList[indice[j - 1]].SwapPeriod, curvesList[indice[j - 1]].SwapBasis, curvesList[indice[j - 1]].FXRate);
                        break;
                    }
                }
                double[] results = DeltaGIRR(indiceIssuer, curvesList[i].Currency, maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread,
                    cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks, withJtdVAL, withStochasticRecoveryVAL,
                    hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta);

                for (int j = 0; j < results.Length; j++)
                {
                    girrSensitivities[i, j] = results[j];
                }
            }
            //Computation of Kb across all buckets (currency)
            double[] tenors = { 0.25, 0.5, 1, 2, 3, 5, 10, 15, 20, 30 };
            double[] Kb = new double[indice.Length];
            double[] Sb = new double[indice.Length];
            double gamma = 0.5;// see 21.50
            for (int b = 0; b < indice.Length; b++)
            {
                Sb[b] = 0;
                Kb[b] = 0;
                int limit = (b == indice.Length - 1) ? curvesList.Length : indice[b + 1];
                for (int j = 0; j < 10; j++)
                {
                    for (int i = indice[b]; i < limit; i++)
                    {
                        Sb[b] += girrSensitivities[i, j];
                        for (int k = 0; k < 10; k++)
                        {
                            for (int l = indice[b]; l < limit; l++)
                            {
                                double weight = 1.0;
                                //Console.WriteLine("bucket: "+b+", curve: "+i+"vs curve: "+l+", tenor: " + tenors[j]+"vs tenor: " + tenors[k]);
                                if (l == i & k != j)//same bucket with different tenor and same curve
                                {
                                    weight = Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4);
                                    //Console.WriteLine(Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4));
                                }
                                else if (l != i & k != j)//same bucket with different tenor and different curve
                                {
                                    weight = Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4) * 0.999;
                                    //Console.WriteLine(Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4));
                                }
                                else if (l != i & k == j)//same bucket with same tenor and different curve
                                {
                                    weight = 0.999;
                                }

                                Kb[b] += girrSensitivities[i, j] * girrSensitivities[l, k] * weight;
                            }
                        }
                    }
                }
                Kb[b] = Math.Sqrt(Math.Max(0, Kb[b]));
            }
            // Computation of Delta if needed (i.e. if the result is <0, see 21.4)
            double delta = 0;
            for (int b = 0; b < indice.Length; b++)
            {
                delta += Kb[b] * Kb[b];
                for (int c = 0; c < indice.Length && c != b; c++)
                {
                    delta += gamma * Sb[b] * Sb[c];
                }
            }
            if (delta < 0)
            {
                delta = 0;
                for (int b = 0; b < indice.Length; b++)
                {
                    Sb[b] = Math.Max(Math.Min(Sb[b], Kb[b]), -Kb[b]);
                }
                for (int b = 0; b < indice.Length; b++)
                {
                    delta += Kb[b] * Kb[b];
                    for (int c = 0; c < indice.Length && c != b; c++)
                    {
                        delta += gamma * Sb[b] * Sb[c];
                    }
                }
                delta = Math.Sqrt(delta);
                girrSensitivities[0, 10] = delta;
            }

            return girrSensitivities;
        }

        //Delta CSR PART

//-------------------------------------------------------------------------------------- DELTA CSR PART ----------------------------------------------------------------------------------
        public static double[] DeltaCSRCDOProxy(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard,
            string pricingCurrency, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
           string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
           double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
           double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
           string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)// PV01 used as proxy for CSO1, prohibiting look-through process
        {
            string[] tenors = { "6M", "1Y", "3Y", "5Y", "10Y" };
            int[] months = {6, 12, 36, 60, 120};
            double[] deltaSensitivities = new double[tenors.Length+1];
            double[] weightedSensitivities = new double[tenors.Length];
            int CDOBucketRW = CDOBucket(BucketCompute(numberOfIssuer, names, ratings, sectors));
            double[] riskWeightsCDO = { 0.04, 0.04, 0.08, 0.05, 0.04, 0.03, 0.02, 0.06, 0.13, 0.13, 0.16, 0.1, 0.12, 0.12, 0.12, 0.13 };// CAUTION Exception for Indices, HAS TO BE DONE

            double nonShocked = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL)[0, 0]);//CDO tranche's NPV non shocked
            for(int i = 0;i < months.Length; i++)
            {
                double shocked = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier,
                dBeta, months[i],pricingCurrency)[0, 0]);//CDO tranche's NPV non shocked
                deltaSensitivities[i] = (shocked - nonShocked) / 0.0001;
                weightedSensitivities[i] = deltaSensitivities[i] * riskWeightsCDO[CDOBucketRW - 1];
            }

            double KbSecCtp = 0;

            for (int col1 = 0; col1 < tenors.Length; col1++)
            {
                if (CDOBucketRW == 16)
                {
                    KbSecCtp += Math.Abs(weightedSensitivities[col1]);
                    break;
                }
                for (int row2 = 0; row2 < numberOfIssuer; row2++)
                {
                    for (int col2 = 0; col2 < tenors.Length; col2++)
                    {
                        double correlationParameterSECCTP = 1.0;
                        if (col1 != col2)//
                        {
                            correlationParameterSECCTP *= 0.65;
                        }
                        KbSecCtp += weightedSensitivities[col1] * weightedSensitivities[col2] * correlationParameterSECCTP;
                    }
                }
            }
            
            KbSecCtp = Math.Sqrt(Math.Max(0, KbSecCtp));//21.4 (4)
            double deltaSECCTP = Math.Abs(KbSecCtp);// 21.4 (4), only one bucket
            deltaSensitivities[tenors.Length] = deltaSECCTP;

            return deltaSensitivities;
        }
        public static double[,] DeltaCSRCDO(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard,
            string pricingCurrency, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
           string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
           double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
           double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
           string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            int[] indices = Fill(paramDate, issuerList, CDSRollDate, alterMode, intensity, cpnPeriod, cpnConvention);
            string[] tenors = { "6M", "1Y", "3Y", "5Y", "10Y" };
            double[,] deltaSensitivities = new double[issuerList.Length, tenors.Length + 1];
            double[,] weightedSensitivities = new double[issuerList.Length, tenors.Length];

            double[] curve;
            StrippingCDS.CDSCurveList curveList = StrippingCDS.CreditDefaultSwapCurves;
            StrippingCDS.CDSCurve CDScurve;

            double shockedCSR = 0.0001;
            double nonShockedCDO = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL)[0, 0]);//CDO tranche's NPV non shocked
            double shockedCDO;

            int CDOBucketRW = CDOBucket(BucketCompute(numberOfIssuer, names, ratings, sectors));
            double[] riskWeightsCDO = { 0.04, 0.04, 0.08, 0.05, 0.04, 0.03, 0.02, 0.06, 0.13, 0.13, 0.16, 0.1, 0.12, 0.12, 0.12, 0.13 };// CAUTION Exception for Indices, HAS TO BE DONE

            for (int i = 0; i < issuerList.Length; i++)//Compute delta sensitivities
            {
                CDScurve = curveList.Curves[StrippingCDS.GetCDSCurveId(issuerList[i])];
                curve = CDScurve.CDSSpread;
                for (int j = 0; j < tenors.Length; j++)
                {
                    if (j == 0)//we shock the 6M
                    {
                        curve[indices[j]] += shockedCSR;
                    }
                    else//we shock another tenor than 6m, so we remove the previous shock
                    {
                        curve[indices[j - 1]] -= shockedCSR;
                        curve[indices[j]] += shockedCSR;
                    }
                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[i]), issuerList[i], paramDate, CDSRollDate, curve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                    shockedCDO = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                        betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL)[0, 0]);
                    deltaSensitivities[i, j] = (shockedCDO - nonShockedCDO) / shockedCSR;
                    weightedSensitivities[i, j] = deltaSensitivities[i, j] * riskWeightsCDO[CDOBucketRW - 1];
                }
                curve[indices[tenors.Length - 1]] -= shockedCSR;
                StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[i]), issuerList[i], paramDate, CDSRollDate, curve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);//Reset all changes after shocking 10Y
            }

            double KbSecCtp = 0;
            for (int row1 = 0; row1 < numberOfIssuer; row1++)//Aggregation within bucket for CSR SEC CTP (only one bucket because one tranche)
            {
                for (int col1 = 0; col1 < tenors.Length; col1++)
                {
                    if (CDOBucketRW == 16)
                    {
                        KbSecCtp += Math.Abs(weightedSensitivities[row1, col1]);
                        break;
                    }
                    for (int row2 = 0; row2 < numberOfIssuer; row2++)
                    {
                        for (int col2 = 0; col2 < tenors.Length; col2++)
                        {
                            double correlationParameterSECCTP = 1.0;
                            if (row1 != row2)
                            {
                                correlationParameterSECCTP *= 0.35 * 0.99;// See 21.60
                            }
                            if (col1 != col2)
                            {
                                correlationParameterSECCTP *= 0.65;
                            }
                            KbSecCtp += weightedSensitivities[row1, col1] * weightedSensitivities[row2, col2] * correlationParameterSECCTP;
                        }
                    }
                }
            }
            KbSecCtp = Math.Sqrt(Math.Max(0, KbSecCtp));//21.4 (4)
            double deltaSECCTP = Math.Abs(KbSecCtp);// 21.4 (4), only one bucket
            deltaSensitivities[0, tenors.Length] = deltaSECCTP;
            return deltaSensitivities;
        }
        public static double[,] DeltaCSRCDS(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity,
        double[] spreadStandard, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, string cpnPeriod, string cpnConvention, string cpnLastSettle)
        {
            int[] indices = Fill(paramDate, issuerList, CDSRollDate, alterMode, intensity, cpnPeriod, cpnConvention);
            string[] tenors = { "6M", "1Y", "3Y", "5Y", "10Y" };

            double[,] deltaSensitivities = new double[issuerList.Length, tenors.Length + 1];
            double[,] weightedSensitivities = new double[issuerList.Length, tenors.Length];

            double nonShockedCDS;
            double shockedCDS;
            double shockedCSR = 0.0001;

            int[] bucket = BucketCompute(numberOfIssuer, names, ratings, sectors);
            double[] riskWeightsCDS = { 0.005, 0.01, 0.05, 0.03, 0.03, 0.02, 0.015, 0.025, 0.02, 0.04, 0.12, 0.07, 0.085, 0.055, 0.05, 0.12, 0.015, 0.05 };

            double[] curve;
            StrippingCDS.CDSCurveList curveList = StrippingCDS.CreditDefaultSwapCurves;
            StrippingCDS.CDSCurve CDScurve;

            for (int i = 0; i < issuerList.Length; i++)
            {
                CDScurve = curveList.Curves[StrippingCDS.GetCDSCurveId(issuerList[i])];
                curve = CDScurve.CDSSpread;
                nonShockedCDS = Double.Parse(ModelInterface.CDS(issuerList[i], maturity, spreadStandard[i] * shockedCSR, CDScurve.Recovery, nominalIssuer[i], cpnPeriod, cpnConvention, cpnLastSettle, CDScurve.Currency, 0, 0, 1, 1)[0, 0]);
                for (int j = 0; j < tenors.Length; j++)
                {
                    if (j == 0)//we shock the 6M
                    {
                        curve[indices[j]] += shockedCSR;
                    }
                    else//we shock another tenor than 6m, so we remove the previous shock
                    {
                        curve[indices[j - 1]] -= shockedCSR;
                        curve[indices[j]] += shockedCSR;
                    }
                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[i]), issuerList[i], paramDate, CDSRollDate, curve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                    shockedCDS = Double.Parse(ModelInterface.CDS(issuerList[i], maturity, spreadStandard[i] * shockedCSR, CDScurve.Recovery, nominalIssuer[i], cpnPeriod, cpnConvention, cpnLastSettle, CDScurve.Currency, 0, 0, 1, 1)[0, 0]);
                    deltaSensitivities[i, j] = (shockedCDS - nonShockedCDS) / shockedCSR;
                    weightedSensitivities[i, j] = deltaSensitivities[i, j] * riskWeightsCDS[bucket[i] - 1];
                }
                curve[indices[tenors.Length - 1]] -= shockedCSR;
                StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[i]), issuerList[i], paramDate, CDSRollDate, curve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);//Reset all changes after shocking 10Y
            }
            double[] KbNonSec = new double[18];
            double[] SbNonSec = new double[18];//See 21.4 (5.b)
            for (int bucketNumber = 0; bucketNumber < 18; bucketNumber++)//Aggregation within bucket for CSR NON SEC
            {
                SbNonSec[0] = 0;
                KbNonSec[bucketNumber] = 0;
                int[] bucketIndice = UtilityLittleFunctions.PositionElement(bucket, bucketNumber + 1);
                for (int row1 = 0; row1 < bucketIndice.Length; row1++)
                {
                    for (int column1 = 0; column1 < tenors.Length; column1++)//If yes, we look for the values
                    {
                        SbNonSec[bucketNumber] += weightedSensitivities[bucketIndice[row1], column1];
                        if (bucketNumber + 1 == 16)//See 21.56
                        {
                            KbNonSec[bucketNumber] += Math.Abs(weightedSensitivities[bucketIndice[row1], column1]);
                            break;
                        }
                        for (int row2 = 0; row2 < bucketIndice.Length; row2++)
                        {
                            for (int column2 = 0; column2 < tenors.Length; column2++)
                            {
                                double correlationNonSec = 1.0;
                                if (bucketNumber + 1 <= 15)//see 21.54
                                {
                                    if (bucketIndice[row1] != bucketIndice[row2])//If names are differents, 35% is applied and 99.90% because curve and name are not differentiated here (for CDS), 99.00% if CDO (see 21.60)
                                    {
                                        correlationNonSec *= 0.35 * 0.999;
                                    }
                                    if (column1 != column2)//If tenor are differents, 65% is applied
                                    {
                                        correlationNonSec *= 0.65;
                                    }
                                }
                                if (bucketNumber + 1 > 16)//see 21.55 (if indices, CAUTION, not made yet)
                                {
                                    if (bucketIndice[row1] != bucketIndice[row2])//If names are differents, 80% is applied and 99.90% because curve and name are not differentiated here
                                    {
                                        correlationNonSec *= 0.80 * 0.999;
                                    }
                                    if (column1 != column2)//If tenor are differents, 65% is applied
                                    {
                                        correlationNonSec *= 0.65;
                                    }
                                }
                                KbNonSec[bucketNumber] += weightedSensitivities[bucketIndice[row1], column1] * weightedSensitivities[bucketIndice[row2], column2] * correlationNonSec;
                            }
                        }
                    }

                }
                KbNonSec[bucketNumber] = Math.Sqrt(Math.Max(0, KbNonSec[bucketNumber]));
            }

            //Across bucket aggregation, delta part
            double[,] correlationMatrixSector =
                {{1   ,0.75,0.10,0.20,0.25,0.20,0.15,0.10,0   ,0.45,0.45},
                 {0.75,1   ,0.05,0.15,0.20,0.15,0.10,0.10,0   ,0.45,0.45},
                 {0.10,0.05,1   ,0.05,0.15,0.20,0.05,0.20,0   ,0.45,0.45},
                 {0.20,0.15,0.05,1   ,0.20,0.25,0.05,0.05,0   ,0.45,0.45},
                 {0.25,0.20,0.15,0.20,1   ,0.25,0.05,0.15,0   ,0.45,0.45},
                 {0.20,0.15,0.20,0.25,0.25,1   ,0.05,0.20,0   ,0.45,0.45},
                 {0.15,0.10,0.05,0.05,0.05,0.05,1   ,0.05,0   ,0.45,0.45},
                 {0.10,0.10,0.20,0.05,0.15,0.20,0.05,1   ,0   ,0.45,0.45},
                 {0   ,0   ,0   ,0   ,0   ,0   ,0   ,0   ,1   ,0   ,0   },
                 {0.45,0.45,0.45,0.45,0.45,0.45,0.45,0.45,0   ,1   ,0.75},
                 {0.45,0.45,0.45,0.45,0.45,0.45,0.45,0.45,0   ,0.75,1   }
            };//See 21.57 (2) table 5

            double deltaNonSec = 0;
            double ratingCorrel = 0;
            double sectorCorrel = 0;

            int k;
            int l;

            double[] SbNonSecDelta = new double[18];
            double dNonSec = 0;

            for (int b = 0; b < 18; b++)
            {
                deltaNonSec += KbNonSec[b] * KbNonSec[b];
                dNonSec += KbNonSec[b] * KbNonSec[b];
                SbNonSecDelta[b] = Math.Max(Math.Min(SbNonSec[b], KbNonSec[b]), -KbNonSec[b]);
                if (b + 1 >= 9 && b + 1 <= 15)
                {
                    k = b - 8;
                }
                else if (b + 1 >= 16)
                {
                    k = b - 7;
                }
                else
                {
                    k = b;
                }
                for (int c = 0; c < 18 && c != b; c++)
                {
                    SbNonSecDelta[c] = Math.Max(Math.Min(SbNonSec[c], KbNonSec[c]), -KbNonSec[c]);

                    if (c + 1 >= 9 && c + 1 <= 15)
                    {
                        l = c - 8;
                    }
                    else if (c + 1 >= 16)
                    {
                        l = c - 7;
                    }
                    else
                    {
                        l = c;
                    }

                    if ((b + 1 <= 15 && c + 1 <= 15) && ((b + 1 >= 9 && c + 1 <= 8) || (b + 1 <= 8 && c + 1 >= 9))) //See 21.57
                    {
                        ratingCorrel = 0.5;
                    }
                    else
                    {
                        ratingCorrel = 1.0;
                    }
                    sectorCorrel = correlationMatrixSector[k, l];
                    deltaNonSec += SbNonSec[b] * SbNonSec[c] * ratingCorrel * sectorCorrel;
                    dNonSec += SbNonSecDelta[b] * SbNonSecDelta[c] * ratingCorrel * sectorCorrel;

                }
            }
            dNonSec = Math.Sqrt(dNonSec);

            if (deltaNonSec < 0)
            {
                deltaNonSec = dNonSec;
            }
            deltaSensitivities[0, tenors.Length] = deltaNonSec;
            return deltaSensitivities;
        }
        //----------------------------------------------------------------------------- FUNCTION FOR ASSESSING RATINGS, SECTORS .. -----------------------------------------------------------------------
        public static int CDOBucket(int[] bucket)//Function which return the bucket for the cdo's tranche
        {
            int[] list = new int[18];
            int indiceMax = bucket[0];
            for (int i = 0; i < bucket.Length; i++)
            {
                list[bucket[i] - 1]++;
                if (list[bucket[i] - 1] > list[indiceMax])
                {
                    indiceMax = bucket[i];
                }
            }
            return indiceMax;
        }

        public static int[] Fill(DateTime paramDate, string[] issuerList, DateTime CDSRollDate, bool alterMode, string intensity, string cpnPeriod, string cpnConvention)
        {
            StrippingCDS.CDSCurveList curveList = StrippingCDS.CreditDefaultSwapCurves;
            string[] curveMaturity = curveList.Curves[1].CurveDates;
            string[] tenors = { "6M", "1Y", "3Y", "5Y", "10Y" };
            int[] indices = new int[tenors.Length];
            int k = 0;
            for (int i = 0; i < tenors.Length; i++)
            {
                for (int j = 0; j < curveMaturity.Length; j++)
                {
                    if (String.Equals(curveMaturity[j], tenors[i]))
                    {
                        indices[k] = j;
                        k++;
                        break;
                    }
                }
            }
            double[] curve = new double[curveMaturity.Length];
            StrippingCDS.CDSCurve CDScurve;
            bool isDone = false;
            for (int i = 0; i < issuerList.Length; i++)
            {
                CDScurve = curveList.Curves[StrippingCDS.GetCDSCurveId(issuerList[i])];
                curve = CDScurve.CDSSpread;
                for (int j = 0; j < tenors.Length; j++)
                {
                    if (curve[indices[j]] == 0)
                    {
                        curve[indices[j]] = Double.Parse(ModelInterface.CDS(issuerList[i], tenors[j], 0, CDScurve.Recovery, 1, cpnPeriod, cpnConvention, "", CDScurve.Currency, 0, 0, 1, 1, 0)[3, 0]);
                        isDone = true;
                    }
                }
                if (isDone)
                {
                    isDone = false;
                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[i]), issuerList[i], paramDate, CDSRollDate, curve, curveMaturity, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                }
            }
            return indices;
        }
        public static string Rating(string rating)
        {
            rating = rating.ToUpper();
            string[] ratings = { "AAA", "AA+", "AA", "AA-", "A+", "A", "A-", "BBB+", "BBB", "BBB-", "BB+", "BB", "BB-", "B+", "B", "B-", "CCC+", "CCC", "CCC-", "CC", "C", "D" };
            for (int i = 0; i < ratings.Length; i++)
            {
                if (String.Equals(rating, ratings[i]))
                {
                    if (i <= 9)
                    {
                        return "IG";//Investment grade
                    }
                    else if (i > 9 && i != ratings.Length - 1)
                    {
                        return "HY";//High yield
                    }
                    else
                    {
                        return "D";//defaulted
                    }

                }
            }
            return "NR";//Non rated
        }
        public static int[] BucketCompute(int numberOfIssuer, string[] issuerName, string[] ratings, double[] sectors)//Compute all the bucket 
        {
            int[] bucket = new int[numberOfIssuer];
            if (sectors[0] == 8 || sectors[0] == 16)
            {
                bucket[0] = (int)sectors[0];
            }
            else if (String.Equals(Rating(ratings[0]), "IG"))
            {
                bucket[0] = (int)sectors[0];
            }
            else
            {
                bucket[0] = (int)sectors[0] + 8;
            }
            if (numberOfIssuer > 1)
            {
                int k = 0;
                for (int i = 1; i < issuerName.Length; i++)
                {
                    if (!String.Equals(issuerName[i], issuerName[i - 1]))
                    {
                        k++;
                        if (sectors[i] == 8 || sectors[i] == 16)
                        {
                            bucket[k] = (int)sectors[i];
                        }
                        else if (String.Equals(Rating(ratings[i]), "IG"))
                        {
                            bucket[k] = (int)sectors[i];
                        }
                        else
                        {
                            bucket[k] = (int)sectors[i] + 8;
                        }
                    }
                }
            }
            return bucket;
        }
//-------------------------------------------------------------------------- CURVATURE PART --------------------------------------------------------------------------------
        public static double curvatureCSR(double floor, string riskClass, double[,] deltaSensitivities, string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate,
    bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency, int numberOfIssuer, string[] issuerList,
    double[] nominalIssuer, double spread, string cpnPeriod, string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, string correlationScenario = "MEDIUM", double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0,
    double[] hedgingCDS = null, double? lossUnitAmount = null, string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            double curvatureRisk = 0;
            int[] indices = Fill(paramDate, issuerList, CDSRollDate, alterMode, intensity, cpnPeriod, cpnConvention);
            int[] bucket = BucketCompute(numberOfIssuer, names, ratings, sectors);
            int bucketSECCTP = CDOBucket(bucket);
            StrippingCDS.CDSCurveList curveList = StrippingCDS.CreditDefaultSwapCurves;
            StrippingCDS.CDSCurve CDScurve;
            double[] upWardCurve;
            double[] downWardCurve;
            double[] originalCurve;
            double shockedCSR = 0.0001;

            bool isSECCTP;
            double[] riskWeights;
            if (String.Equals(riskClass.ToUpper(), "SECCTP"))
            {
                isSECCTP = true;
                riskWeights = new double[] { 0.04, 0.04, 0.08, 0.05, 0.04, 0.03, 0.02, 0.06, 0.13, 0.13, 0.16, 0.1, 0.12, 0.12, 0.12, 0.13 };
            }
            else
            {
                isSECCTP = false;
                riskWeights = new double[] { 0.005, 0.01, 0.05, 0.03, 0.03, 0.02, 0.015, 0.025, 0.02, 0.04, 0.12, 0.07, 0.085, 0.055, 0.05, 0.12, 0.015, 0.05 };
            }

            double[,] curvature = new double[numberOfIssuer, 2];// 2 for the upward and downward shock

            double sik;
            double nonShocked;
            double upWardShocked;
            double downWardShocked;

            if (isSECCTP)
            {
                nonShocked = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta)[0, 0]);//CDO tranche's NPV non shocked
            }
            for (int issuer = 0; issuer < numberOfIssuer; issuer++)
            {
                sik = 0;
                nonShocked = 0;
                upWardShocked = 0;
                downWardShocked = 0;

                for (int column = 0; column < deltaSensitivities.GetLength(1); column++)//s_ik, sum of all delta sensitivities 
                {
                    sik += deltaSensitivities[issuer, column];
                }

                CDScurve = curveList.Curves[StrippingCDS.GetCDSCurveId(issuerList[issuer])];
                downWardCurve = CDScurve.CDSSpread.ToArray();
                upWardCurve = CDScurve.CDSSpread.ToArray();
                originalCurve = CDScurve.CDSSpread.ToArray();

                if (!isSECCTP)
                {
                    nonShocked = Double.Parse(ModelInterface.CDS(issuerList[issuer], maturity, spreadStandard[issuer] * shockedCSR, recoveryIssuer[issuer], nominalIssuer[issuer], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1)[0, 0]);
                    for (int tenors = 0; tenors < indices.Length; tenors++)
                    {
                        upWardCurve[indices[tenors]] += riskWeights[bucket[issuer] - 1];
                        downWardCurve[indices[tenors]] = Math.Max(floor, downWardCurve[indices[tenors]] - riskWeights[bucket[issuer] - 1]);
                    }
                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[issuer]), issuerList[issuer], paramDate, CDSRollDate, upWardCurve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                    upWardShocked = Double.Parse(ModelInterface.CDS(issuerList[issuer], maturity, spreadStandard[issuer] * shockedCSR, recoveryIssuer[issuer], nominalIssuer[issuer], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1)[0, 0]);

                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[issuer]), issuerList[issuer], paramDate, CDSRollDate, downWardCurve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                    downWardShocked = Double.Parse(ModelInterface.CDS(issuerList[issuer], maturity, spreadStandard[issuer] * shockedCSR, recoveryIssuer[issuer], nominalIssuer[issuer], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1)[0, 0]);

                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[issuer]), issuerList[issuer], paramDate, CDSRollDate, originalCurve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);

                    curvature[issuer, 0] = -upWardShocked + nonShocked + riskWeights[bucket[issuer] - 1] * sik;
                    curvature[issuer, 1] = -downWardShocked + nonShocked - riskWeights[bucket[issuer] - 1] * sik;

                }
                else
                {
                    for (int tenors = 0; tenors < indices.Length; tenors++)
                    {
                        upWardCurve[indices[tenors]] += riskWeights[bucketSECCTP - 1];
                        downWardCurve[indices[tenors]] = Math.Max(floor, downWardCurve[indices[tenors]] - riskWeights[bucketSECCTP - 1]);
                    }
                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[issuer]), issuerList[issuer], paramDate, CDSRollDate, upWardCurve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                    upWardShocked = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                    betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta)[0, 0]);


                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[issuer]), issuerList[issuer], paramDate, CDSRollDate, downWardCurve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);
                    downWardShocked = Double.Parse(ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol,
                    betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, 0, 0, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta)[0, 0]);

                    StrippingCDS.StripDefaultProbability(StrippingCDS.GetCDSCurveId(issuerList[issuer]), issuerList[issuer], paramDate, CDSRollDate, originalCurve, CDScurve.CurveDates, CDScurve.Currency, CDScurve.Recovery, alterMode, intensity);

                    curvature[issuer, 0] = -upWardShocked + nonShocked + riskWeights[bucketSECCTP - 1] *sik;
                    curvature[issuer, 1] = -downWardShocked + nonShocked - riskWeights[bucketSECCTP - 1] * sik;
                }
                
            }
            double[] kbUpWard, kbDownWard, kb, sbUpWard,sbDownWard;
            int[] scenario;
            if (isSECCTP)//one bucket
            {
                kbUpWard = new double[1];
                kbDownWard = new double[1];
                kbUpWard[0] = 0;
                kbDownWard[0] = 0;
                double psiUpWard, psiDownWard;
                if (bucketSECCTP == 16)//21.56 (2)
                {
                    kb = new double[1];
                    double k1=0;
                    double k2=0;
                    for(int i = 0; i < numberOfIssuer; i++)
                    {
                        k1 += Math.Max(curvature[i, 0],0);
                        k2 += Math.Max(curvature[i, 1],0);
                    }
                    kb[0] = Math.Max(k1, k2);
                }
                else
                {
                    for (int row1 = 0; row1 < numberOfIssuer; row1++)//Determining the KB in the upWard scenario and downWard scenario
                    {
                        for (int row2 = 0; row2 < numberOfIssuer; row2++)
                        {
                            double correlParameter = 1.0;
                            psiUpWard = 1.0;
                            psiDownWard = 1.0;
                            if (row1 != row2)
                            {
                                if (curvature[row1, 0] < 0 && curvature[row2, 0] < 0)
                                {
                                    psiUpWard = 0;
                                }
                                if (curvature[row1, 0] < 0 && curvature[row2, 0] < 0)
                                {
                                    psiDownWard = 0;
                                }
                                correlParameter *= 0.35;
                                if (String.Equals(correlationScenario.ToUpper(), "HIGH"))//21.6
                                {
                                    correlParameter = Math.Min(1.25 * correlParameter, 1.0);
                                }
                                else if (String.Equals(correlationScenario.ToUpper(), "LOW"))//21.6
                                {
                                    correlParameter = Math.Max(2 * correlParameter - 1.0, 0.75 * correlParameter);
                                }
                                correlParameter *= correlParameter;//21.100

                                kbUpWard[0] += curvature[row1, 0] * curvature[row2, 0] * correlParameter * psiUpWard;//21.5 (b)
                                kbDownWard[0] += curvature[row1, 1] * curvature[row2, 1] * correlParameter * psiDownWard;
                            }
                            else
                            {
                                kbUpWard[0] += Math.Max(curvature[row1, 0], 0) * Math.Max(curvature[row1, 0], 0);
                                kbDownWard[0] += Math.Max(curvature[row1, 1], 0) * Math.Max(curvature[row1, 1], 0);
                            }

                        }
                    }
                    kbUpWard[0] = Math.Sqrt(Math.Max(kbUpWard[0], 0));
                    kbDownWard[0] = Math.Sqrt(Math.Max(kbDownWard[0], 0));

                    kb = new double[1];//21.5 (a)
                    kb[0] = Math.Max(kbDownWard[0], kbUpWard[0]); //only one bucket, no need of determining upWard, downWard scenario
                }
                curvatureRisk = Math.Sqrt(Math.Max(kb[0] * kb[0],0));
            }
            else
            {
                kbUpWard = new double[18];
                kbDownWard = new double[18];
                sbDownWard = new double[18];
                sbUpWard = new double[18];
                kb = new double[18];
                scenario = new int[18];
                double[] sb = new double[18];
                for (int bucketNumber = 0; bucketNumber < 18; bucketNumber++)//Aggregatin within bucket
                {
                    sbDownWard[bucketNumber] = 0;
                    sbUpWard[bucketNumber] = 0;
                    kbUpWard[bucketNumber] = 0;
                    kbDownWard[bucketNumber] = 0;
                    int[] bucketIndices = UtilityLittleFunctions.PositionElement(bucket, bucketNumber + 1);

                    if(bucketNumber != 15)
                    {
                        for (int indice1 = 0; indice1 < bucketIndices.Length; indice1++)
                        {
                            sbUpWard[bucketNumber] += curvature[bucketIndices[indice1], 0];
                            sbDownWard[bucketNumber] += curvature[bucketIndices[indice1], 1];
                            for (int indice2 = 0; indice2 < bucketIndices.Length; indice2++)
                            {
                                double correlParameter = 1.0;
                                double psiUpWard = 1.0;//21.5 (b)
                                double psiDownWard = 1.0;
                                if (bucketIndices[indice1] != bucketIndices[indice2])//names are different, (no basis and tenor in correlation)
                                {
                                    if (curvature[bucketIndices[indice1], 0] < 0 && curvature[bucketIndices[indice2], 0] < 0)
                                    {
                                        psiUpWard = 0;
                                    }
                                    if (curvature[bucketIndices[indice1], 1] < 0 && curvature[bucketIndices[indice2], 1] < 0)
                                    {
                                        psiDownWard = 0;
                                    }
                                    correlParameter *= 0.35;
                                    if (String.Equals(correlationScenario.ToUpper(), "HIGH"))//21.6
                                    {
                                        correlParameter = Math.Min(1.25 * correlParameter, 1.0);
                                    }
                                    else if (String.Equals(correlationScenario.ToUpper(), "LOW"))//21.6
                                    {
                                        correlParameter = Math.Max(2 * correlParameter - 1.0, 0.75 * correlParameter);
                                    }
                                    correlParameter *= correlParameter;//21.100

                                    kbUpWard[bucketNumber] += curvature[bucketIndices[indice1], 0] * curvature[bucketIndices[indice2], 0] * correlParameter * psiUpWard;//21.5 (b)
                                    kbDownWard[bucketNumber] += curvature[bucketIndices[indice1], 1] * curvature[bucketIndices[indice2], 1] * correlParameter * psiDownWard;
                                }
                                else //both names are equal
                                {
                                    kbUpWard[bucketNumber] += Math.Max(curvature[bucketIndices[indice1], 0], 0) * Math.Max(curvature[bucketIndices[indice1], 0], 0);
                                    kbDownWard[bucketNumber] += Math.Max(curvature[bucketIndices[indice1], 1], 0) * Math.Max(curvature[bucketIndices[indice1], 1], 0);
                                }
                            }
                        }
                        kbUpWard[bucketNumber] = Math.Sqrt(Math.Max(0, kbUpWard[bucketNumber]));
                        kbDownWard[bucketNumber] = Math.Sqrt(Math.Max(0, kbDownWard[bucketNumber]));

                        //Determining KB
                        if (kbUpWard[bucketNumber] < kbDownWard[bucketNumber])
                        {
                            kb[bucketNumber] = kbDownWard[bucketNumber];
                            scenario[bucketNumber] = 1;
                            sb[bucketNumber] = sbDownWard[bucketNumber];
                        }
                        else if (kbUpWard[bucketNumber] > kbDownWard[bucketNumber])
                        {
                            kb[bucketNumber] = kbUpWard[bucketNumber];
                            scenario[bucketNumber] = 0;
                            sb[bucketNumber] = sbUpWard[bucketNumber];
                        }
                        else
                        {
                            kb[bucketNumber] = kbUpWard[bucketNumber];

                            double s1 = 0;
                            double s2 = 0;
                            for (int i = 0; i < bucketIndices.Length; i++)
                            {
                                s1 += curvature[bucketIndices[i], 0];
                                s2 += curvature[bucketIndices[i], 1];
                            }
                            if (s1 > s2)
                            {
                                scenario[bucketNumber] = 0;
                                sb[bucketNumber] = sbUpWard[bucketNumber];
                            }
                            else
                            {
                                scenario[bucketNumber] = 1;
                                sb[bucketNumber] = sbDownWard[bucketNumber];
                            }
                        }
                    }
                    else//21.56 (2)
                    {
                        double k1 = 0;
                        double k2 = 0;
                        for(int i = 0; i < bucketIndices.Length; i++)
                        {
                            k1 += Math.Max(curvature[bucketIndices[i], 0],0);
                            k2 += Math.Max(curvature[bucketIndices[i], 1], 0);
                            sbUpWard[bucketNumber] += curvature[bucketIndices[i], 0];
                            sbDownWard[bucketNumber] += curvature[bucketIndices[i], 1];
                        }
                        kb[bucketNumber] = Math.Max(k1, k2);
                        if (k1 > k2)
                        {
                            scenario[bucketNumber] = 0;
                            sb[bucketNumber] = sbUpWard[bucketNumber];
                        }
                        else
                        {
                            scenario[bucketNumber] = 1;
                            sb[bucketNumber] = sbDownWard[bucketNumber];
                        }
                    }
                    
                }
                //Aggregation across bucket
                double[,] correlationMatrixSector =
                     {{1   ,0.75,0.10,0.20,0.25,0.20,0.15,0.10,0   ,0.45,0.45},
                     {0.75,1   ,0.05,0.15,0.20,0.15,0.10,0.10,0   ,0.45,0.45},
                     {0.10,0.05,1   ,0.05,0.15,0.20,0.05,0.20,0   ,0.45,0.45},
                     {0.20,0.15,0.05,1   ,0.20,0.25,0.05,0.05,0   ,0.45,0.45},
                     {0.25,0.20,0.15,0.20,1   ,0.25,0.05,0.15,0   ,0.45,0.45},
                     {0.20,0.15,0.20,0.25,0.25,1   ,0.05,0.20,0   ,0.45,0.45},
                     {0.15,0.10,0.05,0.05,0.05,0.05,1   ,0.05,0   ,0.45,0.45},
                     {0.10,0.10,0.20,0.05,0.15,0.20,0.05,1   ,0   ,0.45,0.45},
                     {0   ,0   ,0   ,0   ,0   ,0   ,0   ,0   ,1   ,0   ,0   },
                     {0.45,0.45,0.45,0.45,0.45,0.45,0.45,0.45,0   ,1   ,0.75},
                     {0.45,0.45,0.45,0.45,0.45,0.45,0.45,0.45,0   ,0.75,1   }
                 };//See 21.57 (2) table 5
                for (int i = 0; i < 18; i++)
                {
                    int k, l;
                    if (i + 1 >= 9 && i + 1 <= 15)//determining sector
                    {
                        k = i - 8;
                    }
                    else if (i + 1 >= 16)
                    {
                        k = i - 7;
                    }
                    else
                    {
                        k = i;
                    }
                    for (int j = 0; j < 18; j++)
                    {
                        if (j + 1 >= 9 && j + 1 <= 15)//determining sector
                        {
                            l = j - 8;
                        }
                        else if (j + 1 >= 16)
                        {
                            l = j - 7;
                        }
                        else
                        {
                            l = j;
                        }


                        if (i == j)
                        {
                            curvatureRisk += kb[i] * kb[i];
                        }
                        else
                        {
                            double psiCurvature = 1.0;
                            double correlParameter = correlationMatrixSector[k, l];
                            if (sbUpWard[i]<0 && sbDownWard[j] < 0)
                            {
                                psiCurvature = 0;
                            }
                            if ((i + 1 <= 15 && j + 1 <= 15) && ((i + 1 >= 9 && j + 1 <= 8) || (i + 1 <= 8 && j + 1 >= 9))) //See 21.57 (rating correlation)
                            {
                                correlParameter *= 0.5;
                            }

                            if (String.Equals(correlationScenario.ToUpper(), "HIGH"))//21.6
                            {
                                correlParameter = Math.Min(1.25 * correlParameter, 1.0);
                            }
                            else if (String.Equals(correlationScenario.ToUpper(), "LOW"))//21.6
                            {
                                correlParameter = Math.Max(2 * correlParameter - 1.0, 0.75 * correlParameter);
                            }

                            correlParameter *= correlParameter;//21.101

                            curvatureRisk += sb[i] * sb[j] * psiCurvature * correlParameter;
                        }
                    }
                }
                curvatureRisk = Math.Sqrt(Math.Max(0, curvatureRisk));
            }
            Console.WriteLine(curvatureRisk);
            return curvatureRisk;
        }
    }
}
