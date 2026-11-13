//dotnet add package MathNet.Numerics
using System;
using MathNet.Numerics.LinearAlgebra;

class Program
{
    static void Main()
    {
        // Dataset: Intercept + Age + BP + Weight + Height + Sugar + Cholesterol
        double[,] Xdata = {
            {1, 25, 120, 65, 170, 90, 180},
            {1, 30, 125, 70, 172, 95, 190},
            {1, 35, 130, 75, 174, 100, 200},
            {1, 40, 135, 80, 176, 105, 210},
            {1, 45, 140, 85, 178, 110, 220},
            {1, 50, 145, 90, 180, 115, 230}
        };

        double[] ydata = { 3200, 3500, 3800, 4200, 4600, 5000 };

        // Create matrices
        var X = Matrix<double>.Build.DenseOfArray(Xdata);
        var y = Vector<double>.Build.DenseOfArray(ydata);

        // Diagnostic: See why Inverse() failed!
        Console.WriteLine($"Matrix Size: {X.RowCount}x{X.ColumnCount}, Actual Rank: {X.Rank()}\n");

        /* This will not work because the matrix is singular (not full rank), which means it cannot be inverted. 
         * The normal equation requires the matrix (X^T * X) to be invertible, but in this case, it is not. 
         * This can happen if there are linearly dependent columns in the matrix X.
        ////// Normal Equation: m = (X^T X)^(-1) X^T y
        ////var Xt = X.Transpose();
        ////var XtX = Xt * X;
        ////var XtX_inv = XtX.Inverse();
        ////var XtY = Xt * y;
        ////var m = XtX_inv * XtY;
        */

        /* This will work because SVD can handle singular matrices and compute a pseudoinverse, 
         * which allows us to find a solution even when the matrix is not full rank.
        //// FIX: Use SVD (Pseudoinverse) instead of (X^T * X)^-1
        //var m = X.Svd(true).Solve(y);
        */

        /* This will work with Ridge Regression because it adds a small regularization term to the diagonal of (X^T * X), 
         * making it invertible. */
        var Xt = X.Transpose();
        var identity = Matrix<double>.Build.DenseIdentity(X.ColumnCount);
        var XtX_ridge = (Xt * X) + (1e-5 * identity);
        var m = XtX_ridge.Inverse() * (Xt * y);


        // Output coefficients
        Console.WriteLine("Intercept: " + m[0]);
        for (int i = 1; i < m.Count; i++)
        {
            Console.WriteLine($"Slope for variable {i}: {m[i]}");
        }

        // Predict for a new sample
        double[] newSample = {1, 42, 138, 82, 177, 107, 215}; // Example
        var newVector = Vector<double>.Build.DenseOfArray(newSample);
        double predictedPremium = m.DotProduct(newVector);

        Console.WriteLine("\nPredicted Premium for new sample: " + predictedPremium);
    }
}
