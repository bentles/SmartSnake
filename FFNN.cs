using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeConsole
{
    class FFNN
    {
        private int num_inputs; // number input nodes
        private int num_hiddens;
        private int num_outputs;
        private int num_weights;

        private double[] inputs;
        private double[][] ihWeights; // input-hidden
        private double[] hBiases;
        private double[] hOutputs;

        private double[][] hoWeights; // hidden-output
        private double[] oBiases;
        private double[] outputs;

        private Random rnd;

        public FFNN(int num_inputs, int num_hiddens, int num_outputs)
        {
            this.num_inputs = num_inputs;
            this.num_hiddens = num_hiddens;
            this.num_outputs = num_outputs;
            this.num_weights = (num_inputs * num_hiddens) + num_hiddens + (num_hiddens * num_outputs) + num_outputs;

            this.inputs = new double[num_inputs];

            this.ihWeights = MakeMatrix(num_inputs, num_hiddens, 0.0);
            this.hBiases = new double[num_hiddens];
            this.hOutputs = new double[num_hiddens];

            this.hoWeights = MakeMatrix(num_hiddens, num_outputs, 0.0);
            this.oBiases = new double[num_outputs];
            this.outputs = new double[num_outputs];

            this.rnd = new Random(0);
        } // ctor

        // ===========================================================================================
        // PROPERTIES - SCOTT
        // ===========================================================================================
        public int nInputs
        {
            get { return num_inputs; }
        }
        public int nOutputs
        {
            get { return num_outputs; }
        }
        public int nHidden
        {
            get { return num_hiddens; }
        }
        public int NumWeights
        {
            get { return num_weights; }
        }
        // ===========================================================================================
        

        private static double[][] MakeMatrix(int rows,
          int cols, double v) // helper for ctor, Train
        {
            double[][] result = new double[rows][];
            for (int r = 0; r < result.Length; ++r)
                result[r] = new double[cols];
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    result[i][j] = v;
            return result;
        }

        //..................................
        public void SetWeights(double[] weights)
        {
            // copy serialized weights and biases in weights[] array
            // to i-h weights, i-h biases, h-o weights, h-o biases
            int numWeights = (num_inputs * num_hiddens) +
              (num_hiddens * num_outputs) + num_hiddens + num_outputs;
            if (weights.Length != numWeights)
                throw new Exception("Bad weights array in SetWeights");

            int k = 0; // points into weights param

            for (int i = 0; i < num_inputs; ++i)
                for (int j = 0; j < num_hiddens; ++j)
                    ihWeights[i][j] = weights[k++];
            for (int i = 0; i < num_hiddens; ++i)
                hBiases[i] = weights[k++];
            for (int i = 0; i < num_hiddens; ++i)
                for (int j = 0; j < num_outputs; ++j)
                    hoWeights[i][j] = weights[k++];
            for (int i = 0; i < num_outputs; ++i)
                oBiases[i] = weights[k++];
        }
        //.........................................
        public double[] GetOutputs()
        {
            double[] hSums = new double[num_hiddens]; // hidden nodes sums scratch array
            double[] oSums = new double[num_outputs]; // output nodes sums

            for (int j = 0; j < num_hiddens; ++j)  // compute i-h sum of weights * inputs
                for (int i = 0; i < num_inputs; ++i)
                    hSums[j] += this.inputs[i] * this.ihWeights[i][j]; // note +=

            for (int i = 0; i < num_hiddens; ++i)  // add biases to hidden sums
                hSums[i] += this.hBiases[i];

            for (int i = 0; i < num_hiddens; ++i)   // apply activation
                this.hOutputs[i] = hSums[i]; // hard-coded

            for (int j = 0; j < num_outputs; ++j)   // compute h-o sum of weights * hOutputs
                for (int i = 0; i < num_hiddens; ++i)
                    oSums[j] += hOutputs[i] * hoWeights[i][j];

            for (int i = 0; i < num_outputs; ++i)  // add biases to output sums
                oSums[i] += oBiases[i];

            double[] softOut = Softmax(oSums); // all outputs at once for efficiency
            Array.Copy(softOut, outputs, softOut.Length);

            double[] retResult = new double[num_outputs]; // could define a GetOutputs 
            Array.Copy(this.outputs, retResult, retResult.Length);
            return retResult;
        }

        public void SetInputs(List<int> inpts)
        {
            for (int i = 0; i < inpts.Count; i++)
            {
                inputs[i] = inpts[i];
            }

        }

        private static double HyperTan(double x)
        {
            if (x < -20.0) return -1.0; // approximation is correct to 30 decimals
            else if (x > 20.0) return 1.0;
            else return Math.Tanh(x);
        }

        private static double[] Softmax(double[] oSums)
        {
            // does all output nodes at once so scale
            // doesn't have to be re-computed each time

            double sum = 0.0;
            for (int i = 0; i < oSums.Length; ++i)
                sum += Math.Exp(oSums[i]);

            double[] result = new double[oSums.Length];
            for (int i = 0; i < oSums.Length; ++i)
                result[i] = Math.Exp(oSums[i]) / sum;

            return result; // now scaled so that xi sum to 1.0
        }
    }
}
