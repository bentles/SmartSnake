using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeConsole
{
    class GA
    {
        // ===========================================================================================
        // VARIABLE INITIALISATION
        private static Random rand = new Random();
        private Game SnakeGame;
        private FFNN NeuralNet;
        private double[] Fitnesses;
        private double[][] HOF;
        private double[] HOF_fit;
        private double[][] Population;
        private double[][] NewPop;
        private int PopSize;
        private int Genes;
        private int TournSize;
        private int[] shuffled;
        private int iShuffled;
        private double[,] geneRange;// [0,] = MAX + [1,] = min
        double bestHOFFit = 0;
        int bestHOFIdx = 0;

        // ===========================================================================================
        // PROPERTIES
        public double ProbMutate { get; set; }
        public double StdDev { get; set; }
        public double SelectMod { get; set; }
        public int NumRuns { get; set; }

        // ===========================================================================================
        //  CONSTRUCTOR
        /*
         * I used this link when I wrote the GA to remember the structure of the program:
         * http://cstheory.stackexchange.com/questions/14758/tournament-selection-in-genetic-algorithms
         * selection modifier could be % of pop chosen for tournament selection... depends on selection implementation
         */
        public GA( Game snake, int pop_size ) 
        {
            // DEFAULT PROPERTY VALUES
            ProbMutate = 0.05;
            StdDev = 0.50;
            SelectMod = 0.10;
            NumRuns = 30;
            // INITIALIZE VARIABLES            
            SnakeGame = (Game)snake;
            NeuralNet = snake.GetNN();
            Genes = NeuralNet.NumWeights;
            PopSize = pop_size;
            Population = new double[PopSize][];
            for (int p = 0; p < pop_size; p++)
			    Population[p] = new double[Genes];
            Fitnesses = new double[PopSize];
            // CREATE AN ARRAY FOR TOURNAMENT SELECTION
            shuffled = new int[pop_size];
            for (int x = 0; x < pop_size; x++) { shuffled[x] = x; }
            ShuffleArray(shuffled); // SHUFFLE IT ONCE BEFORE USE

        }// GA

        // ===========================================================================================
        // METHODS - PUBLIC     

        public void TrainFor(int generations)
        {
            // SETUP PEFORMANCE CAPTURING VARIBLES
            
            HOF = new double[generations][];
            HOF_fit = new double[generations];
            geneRange = new double[2, Genes];
            double bestFit = 0;
            int bestIdx = 0;

            // INITIALISE VARIABLES
            TournSize = (int)(SelectMod*PopSize);
            int idxParent1;
            int idxParent2;
            double[][] Children = new double[2][];
            double[] Selected = new double[Genes];      

            // INITIALISE POPULATION
            InitialPop();
            NewPop = new double[PopSize][];

            // RUN FOR A NUMBER OF GENERATIONS / ITERATIONS
            for (int t = 0; t < generations; t++)
            {
                HOF[t] = new double[Genes];   
                //double[] individual = new double[Genes];
                int count;

                for (int j = 0; j < PopSize; j++)
                {                    
                    for (int k = 0; k < Genes; k++)
                    {// MUTATION - GET GENETIC RANGE / DIVERSITY
                        if ( Population[j][k] > geneRange[0, k] )
                            geneRange[0, k] = Population[j][k];
                        if ( Population[j][k] < geneRange[1, k] )
                            geneRange[1, k] = Population[j][k];
                        //individual[k] = Population[j][k]; 
                    }

                    // FITNESS CALCULATION
                    Fitnesses[j] = EvalFitness( Population[j] );//individual
                    
                    if (Fitnesses[j] > bestFit)
                    {// HALL OF FAME - FIND BEST
                        bestFit = Fitnesses[j];
                        bestIdx = j;
                    }
                }// FOR J

                HOF[t] = (double[])Population[bestIdx].Clone(); // might be pass by reference???
                HOF_fit[t] = Fitnesses[bestIdx];

                if (Fitnesses[bestIdx] > bestHOFFit)
                {
                    bestHOFFit = Fitnesses[bestIdx];
                    bestHOFIdx = t;
                }

                count = 0;
                while (count < PopSize)
                {
                    // SELECTION FOR REPRODUCTION                
                    idxParent1 = Tournament(TournSize);
                    idxParent2 = Tournament(TournSize);
                    double[] parent1 =  Population[idxParent1];
                    double[] parent2 = Population[idxParent2];
            
                    // CROSSOVER
                    Children = Crossover( parent1, parent2 );
                    // MUTATION
                    if (rand.NextDouble() < ProbMutate)
                        Children[0] = MutateGauss(0.05, Children[0]);
                    if (rand.NextDouble() < ProbMutate)
                        Children[1] = MutateGauss(0.05, Children[1]);
                    
                    //REPOPULATION
                    var family = new List<Tuple<double, double[]>>();
                    family.Add(new Tuple<double,double[]>(EvalFitness(Children[0]), Children[0]));
                    family.Add(new Tuple<double,double[]>(EvalFitness(Children[1]), Children[1]));
                    family.Add(new Tuple<double, double[]>(Fitnesses[idxParent1], parent1));
                    family.Add(new Tuple<double, double[]>(Fitnesses[idxParent2], parent2));

                    family.Sort((a, b) => { return a.Item1.CompareTo(b.Item1); });

                    NewPop[count++] = family[2].Item2;
                    NewPop[count++] = family[3].Item2;

                }// WHILE
                Population = (double[][])NewPop.Clone();
                Console.Clear();
                Console.WriteLine("Current Gen's Best: " + HOF_fit[t] + " (" + t + "/" + generations + ")" );
                Console.WriteLine("Best Ever: " + bestHOFFit);
            }//END T
            
        }// TRAINFOR

        public void SetUpBestGame()
        {/* something like: game.getNN, NN.setweights(bestchromo); */               
            NeuralNet.SetWeights( HOF[bestHOFIdx] );
        }// SETUPBESTGAME
                
        // ===========================================================================================
        // FITNESS FUNCTION
        private double EvalFitness( double[] individual )
        {//fitness function runs game 'num_runs' times and gets an average
            NeuralNet.SetWeights( individual );
            int sum = 0;
            for (int i = 0; i < NumRuns; i++)
            {
                sum += SnakeGame.Run();
            }
            return sum / NumRuns;
        }// FITNESS

        // ===========================================================================================
        // METHODS - PRIVATE
        private void InitialPop()
        {
            for (int i = 0; i < PopSize; i++)
            {
                Population[i] = new double[Genes];
                for (int j = 0; j < Genes; j++)
                {
                    Population[i][j] = 2*rand.NextDouble() - 1 ;
                }
            }
        }// INITIALPOP

        private int Tournament(int Size)
        {// RETURN THE INDECIES OF THE CHOSEN PARENTS
            int[] pool = new int[Size];
            double max = 0;
            int Selected = 0;
            for (int i = 0; i < Size; i++ )
            {// RANDOM SELECTION FOR TOURNAMENT - CONTINUE COUNTING
                if (iShuffled == PopSize)
                {// RESHUFFLE THE LIST IF END REACHED
                    ShuffleArray(shuffled);
                    iShuffled = 0;
                }
                pool[i] = shuffled[iShuffled];
                iShuffled++;
                // START TOURNAMENT
                if (Fitnesses[pool[i]] > max)
                {
                    Selected = pool[i];
                    max = Fitnesses[Selected];
                }
            }            
            return Selected;
        }// TOURNAMENT

        private int[] ShuffleArray(int[] list)
        {
            for (int i = list.Length; i > 0; i--)
            {
                int j = rand.Next(i);
                int k = (int)list[j];
                list[j] = (int)list[i - 1];
                list[i - 1] = k;
            }
            return list;
        }// SHUFFLEARRAY

        private double[] MutateUniform(double[] individual)
        {// I would rather implement a Gaussian version of this
            //double[] Mutant = new double[Genes];
            for (int g = 0; g < Genes; g++)
            {
                int r = rand.Next(0,1);
                if ( r == 1 )
                {// delta = max - current
                    individual[g] = individual[g] + ( geneRange[0, g] - individual[g] );
                }
                else
                {// delta = current - min
                    individual[g] = individual[g] + ( individual[g] - geneRange[1, g] );
                } 
            }
            return individual;
        }// MUTATEUNIFORM

        private double[] MutateGauss( double prob, double[] individual)
        {
            for (int g = 0; g < Genes; g++)
            {   
                double Range = geneRange[0, g] - geneRange[1, g];
                if (rand.NextDouble() < prob)
                {// if gene is to mutate                                    
                    individual[g] = individual[g] - RandomGaussian.NextGaussian() * StdDev; //* Range;      
                }
            }
            return individual;
        }// MUTATEGAUSS

        private double[][] Crossover(double[] Parent1, double[] Parent2)
        {// SBX CROSSOVER - WITH A CUSTOM FIX
            double[][] Children = new double[2][];
            Children[0] = new double[Genes];
            Children[1] = new double[Genes];
            int n = 2;
            double r;
            double y;
            for (int i = 0; i < Genes; i++)
            {
                r = rand.NextDouble();
                y = rand.NextDouble();
                if (r <= 0.5)
                    y = (double)Math.Pow( (2*r), (1/(n+1)) );
                else
                    y = (double)Math.Pow( 1/(2*(1-r)), (1/(n+1)) );

                Children[0][i] = 0.5 * ( (1 + y) * Parent1[i] + (1 - y) * Parent2[i] );
                Children[1][i] = 0.5 * ( (1 - y) * Parent1[i] + (1 + y) * Parent2[i] );
            }
            return Children;
        }

    }// CLASS
}// NAMESPACE
