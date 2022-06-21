using System;
using System.Collections;
using System.Linq; // Related to data
using System.Text; // Work with text and coding

namespace RWA_calculator
{
    internal class Program
    {
        static void Main(string[] args)
        {
        double mode;
        Console.WriteLine("Hello! welcome to the menu:");
        Console.WriteLine("Input 1 to evaluate a transaction");
        Console.WriteLine("Input 2 to get a pricing grid");
        Console.WriteLine("input any other character to exit");
        mode = Saisir_Double();

        if (mode == 1)
        {
            double amount;
            double maturity;
            double CCF = 1;
            double pricing;
            bool flatFee = false;
            bool offBalance;
            string rating;

            Console.WriteLine("input the committed amount: ");
            amount = Saisir_Double();
            Console.WriteLine("input the client's rating (Moody's notation): ");
            rating = Saisir_rating();
            Console.WriteLine("input the instrument's maturity (in years): ");
            maturity = Saisir_Double();
            Console.WriteLine("is the instrument off balance? (y/n): ");
            offBalance = yesNoQuestion();
            if (offBalance)
            {
                Console.WriteLine("input the instrument's CCF: ");
                CCF = Saisir_Double();
            }
            Console.WriteLine("is pricing a flat fee? (y/n): ");
            flatFee = yesNoQuestion();
            if (flatFee)
            {
                Console.WriteLine("input the fee (in currency): ");
                pricing = Saisir_Double();
            }
            else
            {
                Console.WriteLine("input the fee (in basis points per annum): ");
                pricing = Saisir_Double();
            }



            //RESULTS
            double rwa_value = RWA(PD(rating), LGD(rating), maturity, EAD(amount, CCF));
            double nbi_value = NBI(amount, pricing, maturity, flatFee);
            double eva_value = EVA(Cost_of_risk(rwa_value, 0.03,maturity), nbi_value);
            double pi_value = PI(rwa_value, nbi_value);
            Console.WriteLine("INSTRUMENT METRICS: ");
            Console.WriteLine("RWA: . . . . . . . . {0:F}", rwa_value);
            Console.WriteLine("NBI: . . . . . . . . {0:F}", nbi_value);
            Console.WriteLine("EVA: . . . . . . . . {0:F}", eva_value);
            Console.WriteLine("PI : . . . . . . . . {0:P}", pi_value);
        }
        else if (mode == 2)
        {
            double CCF = 1;
            bool offBalance;
            Console.WriteLine("is the instrument off balance? (y/n): ");
            offBalance = yesNoQuestion();
            if (offBalance)
            {
                Console.WriteLine("input the instrument's CCF: ");
                CCF = Saisir_Double();
            }
            Console.WriteLine("FLOOR PRICING:");
            string[] Notes = { "Aa3", "A3", "Baa3", "Ba3", "B3", "Caa3" };
            foreach (string rating in Notes)
            {
                double amount = 100000;
                Console.WriteLine("Rating: {0:G}. . . . .", rating);
                for (double maturity = 1; maturity < 10; maturity = maturity + 2 * (maturity % 10))
                {
                    double pricing = 5;// On commence avec 5 points de base par an (5bppa)
                    Console.WriteLine("Maturity: {0:G} years .... ", maturity);
                    double rwa_value = RWA(PD(rating), LGD(rating), maturity, EAD(amount, CCF));
                    double nbi_value = NBI(amount, pricing, maturity);
                    double eva_value = EVA(Cost_of_risk(rwa_value, 0.03,maturity), nbi_value);
                    while (eva_value < 0)
                    {
                        pricing = pricing + 1;
                        nbi_value = NBI(amount, pricing, maturity);
                        eva_value = EVA(Cost_of_risk(rwa_value, 0.03,maturity), nbi_value);
                    }
                    Console.WriteLine("Fee . . . . EVA . . . . PI ");
                    Console.WriteLine("{0:P}. . . . {1:F} . . . {2:P}  ", pricing/10000, eva_value, PI(rwa_value, nbi_value));
                }
            }
        }
    }
    ///////////////////////////////////////////////////////////////////////
    // FONCTIONS POUR INTERAGIR AVEC L'UTILISATEUR ////////////////////////
    static double Saisir_Double()
    {
        try
        {
            return double.Parse(Console.ReadLine());
        }
        catch
        {
            Console.WriteLine("Please Enter a valid input:");
            return Saisir_Double();
        }
    }
    static bool yesNoQuestion()
    {
        string input = Console.ReadLine();
        if (input == "y")
        {
            return true;
        }
        else if (input == "n")
        {
            return false;
        }
        else
        {
            Console.WriteLine("Please Enter a valid input:");
            return yesNoQuestion();
        }
    }
    static string Saisir_rating()
    {
        string[] ratings = { "Aa1", "Aa2", "Aa3", "A1", "A2", "A3", "Baa1", "Baa2", "Baa3", "Ba1", "Ba2", "Ba3", "B1", "B2", "B3", "Caa1", "Caa2", "Caa3" };
        string input = Console.ReadLine();
        if (IsInList_str(ratings, input))
        {
            return input;
        }
        else
        {
            Console.WriteLine("Please Enter a valid input:");
            return Saisir_rating();
        }
    }
    static bool IsInList_str(string[] list, string word)
    {
        foreach (string item in list)
        {
            if (item == word)
            {
                return true;
            }
        }
        return false;
    }

    //////////////////////////////////////////////////////////////////////
    // FONCTIONS STATISTIQUES ////////////////////////////////////////////
    /* Fonction de Repartition de la Distribution Normale Standard
     On utilise l'implémentation de  John D. Cook:
    https://www.johndcook.com/blog/csharp_phi/
    L'implementation est testée en la comparant à la fonction "NORMDIST" d'excel 
    comme recomandé par la BIS */
    static double Phi(double x)
    {
        // constants
        double a1 = 0.254829592;
        double a2 = -0.284496736;
        double a3 = 1.421413741;
        double a4 = -1.453152027;
        double a5 = 1.061405429;
        double p = 0.3275911;

        // Save the sign of x
        int sign = 1;
        if (x < 0)
            sign = -1;
        x = Math.Abs(x) / Math.Sqrt(2.0);

        // A&S formula 7.1.26
        double t = 1.0 / (1.0 + p * x);
        double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return 0.5 * (1.0 + sign * y);
    }
    /* Inverse de la Fonction de Repartition de la Distribution Normale Standard
    On utilise l'algorithme de Peter John Acklam:
    https://web.archive.org/web/20151110174102/http://home.online.no/~pjacklam/notes/invnorm/
    L'algorithme a été testé pour qu'il donne les mêmes résultats que la fonction "NORMSINV" d'excel
    comme recommendé par la BIS */
    static double Norm_Inv_std(double p)
    {
        double[] a = new double[6];
        a[0] = -39.69683028665376;
        a[1] = 220.9460984245205;
        a[2] = -275.9285104469687;
        a[3] = 138.3577518672690;
        a[4] = -30.66479806614716;
        a[5] = 2.506628277459239;

        double[] b = new double[5];
        b[0] = -54.47609879822406;
        b[1] = 161.5858368580409;
        b[2] = -155.6989798598866;
        b[3] = 66.80131188771972;
        b[4] = -13.28068155288572;


        double[] c = new double[6];
        c[0] = -0.007784894002430293;
        c[1] = -0.3223964580411365;
        c[2] = -2.400758277161838;
        c[3] = -2.549732539343734;
        c[4] = 4.374664141464968;
        c[5] = 2.938163982698783;

        double[] d = new double[4];
        d[0] = 0.007784695709041462;
        d[1] = 0.3224671290700398;
        d[2] = 2.445134137142996;
        d[3] = 3.754408661907416;
        double p_low = 0.02425;
        double p_high = 1 - p_low;
        if (0 < p && p < p_low)
        //LOWER REGION
        {
            double q = Math.Sqrt(-2 * Math.Log(p));
            double numerator = c[0] * q;
            numerator = (numerator + c[1]) * q;
            numerator = (numerator + c[2]) * q;
            numerator = (numerator + c[3]) * q;
            numerator = (numerator + c[4]) * q;
            numerator = numerator + c[5];
            double denominator = d[0] * q;
            denominator = (denominator + d[1]) * q;
            denominator = (denominator + d[2]) * q;
            denominator = (denominator + d[3]) * q;
            denominator = denominator + 1;
            return numerator / denominator;
        }
        else if (1 > p && p > p_high)
        //UPPER REGION
        {
            double q = Math.Sqrt(-2 * Math.Log(1 - p));
            double numerator = c[0] * q;
            numerator = (numerator + c[1]) * q;
            numerator = (numerator + c[2]) * q;
            numerator = (numerator + c[3]) * q;
            numerator = (numerator + c[4]) * q;
            numerator = numerator + c[5];
            double denominator = d[0] * q;
            denominator = (denominator + d[1]) * q;
            denominator = (denominator + d[2]) * q;
            denominator = (denominator + d[3]) * q;
            denominator = denominator + 1;
            return -numerator / denominator;
        }
        else if (p <= p_high && p >= p_low)
        //CENTRAL REGION
        {
            double q = p - 0.5;
            double r = q * q;
            double numerator = a[0] * r;
            numerator = (numerator + a[1]) * r;
            numerator = (numerator + a[2]) * r;
            numerator = (numerator + a[3]) * r;
            numerator = (numerator + a[4]) * r;
            numerator = (numerator + a[5]) * q;
            double denominator = b[0] * r;
            denominator = (denominator + b[1]) * r;
            denominator = (denominator + b[2]) * r;
            denominator = (denominator + b[3]) * r;
            denominator = (denominator + b[4]) * r;
            denominator = denominator + 1;
            return numerator / denominator;
        }
        else
            return 99;
    }
    //////////////////////////////////////////////////////////////////////
    // FONCTIONS DE RATING ///////////////////////////////////////////////
    static double PD(string rating)
    {
        switch (rating)
        {
            case "Aa1":
            case "Aa2":
            case "Aa3":
                return 0.0001;
            case "A1":
            case "A2":
            case "A3":
                return 0.001;
            case "Baa1":
            case "Baa2":
            case "Baa3":
                return 0.01;
            case "Ba1":
            case "Ba2":
            case "Ba3":
                return 0.1;
            case "B1":
            case "B2":
            case "B3":
                return 0.2;
            case "Caa1":
            case "Caa2":
            case "Caa3":
                return 0.5;
            default:
                return 1;
        }

    }
    static double LGD(string rating)
    {
        switch (rating)
        {
            case "Aa1":
            case "Aa2":
            case "Aa3":
                return 0.15;
            case "A1":
            case "A2":
            case "A3":
                return 0.20;
            case "Baa1":
            case "Baa2":
            case "Baa3":
                return 0.25;
            case "Ba1":
            case "Ba2":
            case "Ba3":
                return 0.35;
            case "B1":
            case "B2":
            case "B3":
                return 0.45;
            case "Caa1":
            case "Caa2":
            case "Caa3":
                return 0.5;
            default:
                return 1;
        }

    }

    //////////////////////////////////////////////////////////////////////
    // FONCTIONS DE CALCUL RWA - MODELE STANDARD /////////////////////////
    static double Correlation_R(double PD)
    {
        double A = 1 - Math.Exp(-50 * PD);
        double B = 1 - Math.Exp(-50);
        double C = A / B;
        return 0.12 * C + 0.24 * (1 - C);
    }
    static double Maturity_Adjustment(double PD)
    {
        double A = Math.Log(PD);
        double B = 0.118522 - 0.05478 * A;
        return Math.Pow(B, 2);
    }
    static double Capital_Requirement(double PD, double LGD, double R, double M, double b)
    {
        double Mat_correction = (1 + b * (M - 2.5)) / (1 - b * 1.5);
        double A = Math.Sqrt(1 - R);
        double B = (Norm_Inv_std(PD) + Math.Sqrt(R) * Norm_Inv_std(0.999)) / A;
        double C = Phi(B);
        double K = LGD * C - PD * LGD;
        return K * Mat_correction;
    }
    static double EAD(double committed, double CCF = 1)
    {
        return CCF * committed;
    }
    static double RWA(double PD, double LGD, double M, double EAD)
    {
        double R = Correlation_R(PD);
        double b = Maturity_Adjustment(PD);
        double K = Capital_Requirement(PD, LGD, R, M, b);
        return K * 12.5 * EAD;
    }
    //////////////////////////////////////////////////////////////////////
    // FONCTIONS DE CALCUL KPI  /////////////////////////////////////////
    static double NBI(double Amount, double fee, double maturity, bool flat = false)
    {
        if (flat == false) { return Amount * fee/10000 * maturity; }
        else { return fee; }
    }
    static double Cost_of_risk(double RWA, double WACC, double maturity)
    {
        return RWA * WACC* maturity;
    }
    static double EVA(double Cost, double NBI)
    {
        return NBI - Cost;
    }
    static double PI(double RWA, double NBI)
    {
        return NBI / RWA;
    }

    }
}
