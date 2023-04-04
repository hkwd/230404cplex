// --------------------------------------------------------------------------
// File: Dietlesson.cs
// --------------------------------------------------------------------------
// Licensed Materials - Property of IBM
// 5725-A06 5725-A29 5724-Y48 5724-Y49 5724-Y54 5724-Y55 5655-Y21
// Copyright IBM Corporation 2001, 2022. All Rights Reserved.
//
// Note to U.S. Government Users Restricted Rights:
// Use, duplication or disclosure restricted by GSA ADP Schedule
// Contract with IBM Corp.
// --------------------------------------------------------------------------

using ILOG.Concert;
using ILOG.CPLEX;
using static Diet;


//Step 2 create the class

public class Diet
{
    internal class Data
    {
        internal int nFoods;
        internal int nNutrs;
        internal double[] foodCost;
        internal double[] foodMin;
        internal double[] foodMax;
        internal double[] nutrMin;
        internal double[] nutrMax;
        internal double[][] nutrPerFood;

        internal Data(string filename)
        {
            InputDataReader reader = new InputDataReader(filename);

            foodCost = reader.ReadDoubleArray();
            foodMin = reader.ReadDoubleArray();
            foodMax = reader.ReadDoubleArray();
            nutrMin = reader.ReadDoubleArray();
            nutrMax = reader.ReadDoubleArray();
            nutrPerFood = reader.ReadDoubleArrayArray();
            nFoods = foodMax.Length;
            nNutrs = nutrMax.Length;

            if (nFoods != foodMin.Length ||
                 nFoods != foodMax.Length)
                throw new ILOG.Concert.Exception("inconsistent data in file "
                                                 + filename);
            if (nNutrs != nutrMin.Length ||
                 nNutrs != nutrPerFood.Length)
                throw new ILOG.Concert.Exception("inconsistent data in file "
                                                 + filename);
            for (int i = 0; i < nNutrs; ++i)
            {
                if (nutrPerFood[i].Length != nFoods)
                    throw new ILOG.Concert.Exception("inconsistent data in file "
                                                  + filename);
            }
        }
    }
    //Step 6 set up rows

    internal static void BuildModelByRow(IModeler model,
                                         Data data,
                                         INumVar[] Buy,
                                         NumVarType type)
    {
        int nFoods = data.nFoods;
        int nNutrs = data.nNutrs;

        //Step 7 build rows

        for (int j = 0; j < nFoods; j++)
        {
            Buy[j] = model.NumVar(data.foodMin[j], data.foodMax[j], type);
        }

        //Step 8 add objective for rows

        model.AddMinimize(model.ScalProd(data.foodCost, Buy));

        //Step 9 add ranged constraints for rows

        for (int i = 0; i < nNutrs; i++)
        {
            model.AddRange(data.nutrMin[i],
                           model.ScalProd(data.nutrPerFood[i], Buy),
                           data.nutrMax[i]);
        }
    }

    //Step 10 set up columns

    internal static void BuildModelByColumn(IMPModeler model,
                                            Data data,
                                            INumVar[] Buy,
                                            NumVarType type)
    {
        int nFoods = data.nFoods;
        int nNutrs = data.nNutrs;

        //Step 11 add empty columns for obj and ranged constraints

        IObjective cost = model.AddMinimize();
        IRange[] constraint = new IRange[nNutrs];

        for (int i = 0; i < nNutrs; i++)
        {
            constraint[i] = model.AddRange(data.nutrMin[i], data.nutrMax[i]);
        }

        //Step 12 create variables for columns  

        for (int j = 0; j < nFoods; j++)
        {

            Column col = model.Column(cost, data.foodCost[j]);

            for (int i = 0; i < nNutrs; i++)
            {
                col = col.And(model.Column(constraint[i],
                                           data.nutrPerFood[i][j]));
            }

            Buy[j] = model.NumVar(col, data.foodMin[j], data.foodMax[j], type);

        }
    }


    public static void Main(string[] args)
    {


        try
        {

            string filename = "../../../examples/data/diet.dat";
            bool byColumn = false;
            NumVarType varType = NumVarType.Float;


            //Step 16 interpret command line

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToCharArray()[0] == '-')
                {
                    switch (args[i].ToCharArray()[1])
                    {
                        case 'c':
                            byColumn = true;
                            break;
                        case 'i':
                            varType = NumVarType.Int;
                            break;
                        default:
                            Usage();
                            return;
                    }
                }
                else
                {
                    filename = args[i];
                    break;
                }
            }

            Data data = new Data(filename);

            int nFoods = data.nFoods;
            int nNutrs = data.nNutrs;

            //Step 3 create model

            Cplex cplex = new Cplex();

            //Step 4 create variables

            INumVar[] Buy = new INumVar[nFoods];

            //Step 5 indicate by row or by column

            if (byColumn) BuildModelByColumn(cplex, data, Buy, varType);
            else BuildModelByRow(cplex, data, Buy, varType);

            //Step 13 solve
            cplex.SetParam(Cplex.Param.MIP.Limits.Solutions, 1);
            string lpFileName = "DietLesson";
            cplex.ExportModel(lpFileName + ".lp");
            cplex.ExportModel(lpFileName + ".sav");
            cplex.WriteParam(lpFileName + "1.prm");
            cplex.Solve();
            cplex.WriteMIPStarts(lpFileName + ".mst");
            cplex.SetParam(Cplex.Param.MIP.Limits.Solutions, 1000);
            cplex.WriteParam(lpFileName + "2.prm");
            if (cplex.Solve())
            {
                cplex.WriteSolution(lpFileName + ".sol");
                //Step 14 display the solution

                System.Console.WriteLine();
                System.Console.WriteLine("Solution status = "
                                         + cplex.GetStatus());
                System.Console.WriteLine();
                System.Console.WriteLine(" cost = " + cplex.ObjValue);
                for (int i = 0; i < nFoods; i++)
                {
                    System.Console.WriteLine(" Buy"
                                             + i
                                             + " = "
                                             + cplex.GetValue(Buy[i]));
                }
                System.Console.WriteLine();
            }

            //Step 15 free memory

            cplex.End();

            //Step 18 catch

        }
        catch (ILOG.Concert.Exception ex)
        {
            System.Console.WriteLine("Concert Error: " + ex);
        }
        catch (InputDataReader.InputDataReaderException ex)
        {
            System.Console.WriteLine("Data Error: " + ex);
        }
        catch (System.IO.IOException ex)
        {
            System.Console.WriteLine("IO Error: " + ex);
        }
    }

    //Step17 show correct use of command line 

    internal static void Usage()
    {
        System.Console.WriteLine(" ");
        System.Console.WriteLine("usage: Diet [options] <data file>");
        System.Console.WriteLine("options: -c  build model by column");
        System.Console.WriteLine("         -i  use integer variables");
        System.Console.WriteLine(" ");
    }

}
