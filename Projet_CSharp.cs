using System;
using System.Diagnostics;
using System.Collections;
using System.Threading.Tasks;

namespace Projet_CSharp_2019
{
    public enum Resultat { j1gagne, j0gagne, partieNulle, indetermine }

    public abstract class Position
    {
        public bool j1aletrait;
        public Position(bool j1aletrait) { this.j1aletrait = j1aletrait; }
        public Resultat Eval { get; protected set; }
        public int NbCoups { get; protected set; }
        public abstract void EffectuerCoup(int i);
        public abstract Position Clone();
        public abstract void Affiche();
    }

    public abstract class Joueur
    {
        public abstract int Jouer(Position p);
        public virtual void NouvellePartie() { }
    }

    public class Partie
    {
        Position pCourante;
        Joueur j1, j0;
        public Resultat r;

        public Partie(Joueur j1, Joueur j0, Position pInitiale)
        {
            this.j1 = j1;
            this.j0 = j0;
            pCourante = pInitiale.Clone();
        }

        public void NouveauMatch(Position pInitiale)
        {
            pCourante = pInitiale.Clone();
        }

        public void Commencer(bool affichage = true)
        {
            j1.NouvellePartie();
            j0.NouvellePartie();

            do
            {
                if (affichage)
                    pCourante.Affiche();
                if (pCourante.j1aletrait)
                {
                    pCourante.EffectuerCoup(j1.Jouer(pCourante.Clone()));
                }
                else
                {
                    pCourante.EffectuerCoup(j0.Jouer(pCourante.Clone()));
                }
                
            } while (pCourante.NbCoups > 0);
            r = pCourante.Eval;
            
            if (affichage)
            {
                pCourante.Affiche();
                switch (r)
                {
                    case Resultat.j1gagne: Console.WriteLine("j1 {0} a gagné.", j1); break;
                    case Resultat.j0gagne: Console.WriteLine("j0 {0} a gagné.", j0); break;
                    case Resultat.partieNulle: Console.WriteLine("Partie nulle."); break;
                }
            }
        }
    }

    public class Noeud
    {
        static Random gen = new Random();

        public Position p;
        public Noeud pere;
        public Noeud[] fils;
        public int cross, win;
        public int indiceMeilleurFils;

        public Noeud(Noeud pere, Position p)
        {
            this.pere = pere;
            this.p = p;
            fils = new Noeud[this.p.NbCoups];
        }

        public void CalculMeilleurFils(Func<int, int, float> phi)
        {
            float s;
            float sM = 0;
            if (p.j1aletrait)
            {
                for (int i = 0; i < fils.Length; i++)
                {
                    if (fils[i] == null) { s = phi(0, 0); }
                    else { s = phi(fils[i].win, fils[i].cross); }
                    if (s > sM) { sM = s; indiceMeilleurFils = i; }
                }
            }
            else
            {
                for (int i = 0; i < fils.Length; i++)
                {
                    if (fils[i] == null) { s = phi(0, 0); }
                    else { s = phi(fils[i].cross - fils[i].win, fils[i].cross); }
                    if (s > sM) { sM = s; indiceMeilleurFils = i; }
                }
            }
        }


        public Noeud MeilleurFils()
        {
            if (fils[indiceMeilleurFils] != null)
            {
                return fils[indiceMeilleurFils];
            }
            Position q = p.Clone();
            q.EffectuerCoup(indiceMeilleurFils);
            fils[indiceMeilleurFils] = new Noeud(this, q);
            return fils[indiceMeilleurFils];
        }

        public override string ToString()
        {
            string s = "";
            s = s + "indice MF = " + indiceMeilleurFils;
            s += String.Format(" note= {0}\n", fils[indiceMeilleurFils] == null ? "?" : ((1F * fils[indiceMeilleurFils].win) / fils[indiceMeilleurFils].cross).ToString());
            int sc = 0;
            for (int k = 0; k < fils.Length; k++)
            {
                if (fils[k] != null)
                {
                    sc += fils[k].cross;
                    s += (fils[k].win + "/" + fils[k].cross + " ");
                }
                else s += (0 + "/" + 0 + " ");
            }
            s += "\n nbC=" + (sc / 2);
            return s;
        }

    }

    public class JMCTS : Joueur
    {
        public static Random gen = new Random();
        static Stopwatch sw = new Stopwatch();

        float a, b;
        int temps;

        Noeud racine;

        public JMCTS(float a, float b, int temps)
        {
            this.a = 2 * a;
            this.b = 2 * b;
            this.temps = temps;
        }

        public override string ToString()
        {
            return string.Format("JMCTS[{0} - {1} - temps={2}]", a / 2, b / 2, temps);
        }

        int JeuHasard(Position p)
        {
            Position q = p.Clone();
            int re = 1;
            while (q.NbCoups > 0)
            {
                q.EffectuerCoup(gen.Next(0, q.NbCoups));
            }
            //Console.WriteLine("Nbcoups = " + q.NbCoups);
            if (q.Eval == Resultat.j1gagne) { re = 2; }
            if (q.Eval == Resultat.j0gagne) { re = 0; }
            return re;
        }


        public override int Jouer(Position p)
        {
            sw.Restart();
            Func<int, int, float> phi = (W, C) => (a + W) / (b + C);

            racine = new Noeud(null, p);
            int iter = 0;
            while (sw.ElapsedMilliseconds < temps)
            {
                Noeud no = racine;

                do // Sélection
                {
                    no.CalculMeilleurFils(phi);
                    no = no.MeilleurFils();

                } while (no.cross > 0 && no.fils.Length > 0);

                int re = JeuHasard(no.p); // Simulation

                while (no != null) // Rétropropagation
                {
                    no.cross += 2;
                    no.win += re;
                    no = no.pere;
                }
                iter++;
            }
            racine.CalculMeilleurFils(phi);
            Console.WriteLine("{0} itérations", iter);
            Console.WriteLine(racine);
            return racine.indiceMeilleurFils;

        }
    }

    //Partie du travail effectué

    class PositionA : Position
    {
        int NB_allumette;

        //Constructeur de PositionA
        public PositionA(bool j1aletrait ,int NB_allumette_reste) : base(j1aletrait)
        {
            NB_allumette = NB_allumette_reste;
            NbCoups = (NB_allumette >= 3) ? 3 : NB_allumette;
            Eval = Resultat.indetermine; 
        }
        
        //Rédefinition de la méthode EffectuerCoup
        public override void EffectuerCoup(int i)
        {
            NB_allumette = NB_allumette - (i+1);
            NbCoups = (NB_allumette >= 3) ? 3 : NB_allumette;

            if (j1aletrait)
                Eval = (NbCoups != 0) ? Resultat.indetermine : Resultat.j0gagne;
            else
                Eval = (NbCoups != 0) ? Resultat.indetermine : Resultat.j1gagne;

            j1aletrait = !j1aletrait;
            
        }

        //Rédefinition de la méthode Clone
        public override Position Clone()
        {
            return new PositionA(j1aletrait, NB_allumette);
        }

        //Rédefinition de la méthode Affiche
        public override void Affiche()
        {
            if (Eval == Resultat.indetermine)
            {
                for (int i = 0; i < NB_allumette; i++)
                {
                    Console.Write("| ");
                }
                Console.WriteLine("");
                Console.WriteLine("Il reste {0} allumettes.",NB_allumette);
                if (j1aletrait)
                    Console.WriteLine("C'est le tour de Joueur1!");
                else
                    Console.WriteLine("C'est le tour de Joeur0!");
            }
            else
                Console.WriteLine("Le jeu est terminé!");
        }

    }

    class JouerHumainA : Joueur
    {
        Position p;

        public JouerHumainA(Position p)
        {
            this.p = p;
        }

        //Rédefintiion de la methode Jouer
        public override int Jouer(Position p)
        {
            int nb;
 
            Console.WriteLine("Combien prenez-vous d'allumettes?");
            nb = int.Parse(Console.ReadLine());

            while (nb > p.NbCoups || nb <= 0)
            {
                Console.WriteLine("Erro, vous ne pouvez pas d'enlever plus que {0} allumettes ou moins que 1 allumette!", p.NbCoups);
                Console.WriteLine("Combien prenez-vous d'allumettes?");
                nb = int.Parse(Console.ReadLine());
            } 
            return nb-1;
        }

    }

    class PositionP4 : Position
    {
        int[,] tab = new int[6,7];
       

        public PositionP4(bool j1aletrait) : base(j1aletrait)
        {
            NbCoups = 7;
            Eval = Resultat.indetermine;
        }

        public override void EffectuerCoup(int i)//Choisir la colonne ou on veut mettre notre pion dessous
        {
            //Verification pour voir si il y'a des colonnes remplis avant la colonne i et puis decaler l'indice
            for (int col = 0; col <= i; col++)
            {
                if (tab[0, col] != 0)
                    i++;
            }

            if (Eval == Resultat.indetermine)
            {
                for (int lig = 5; lig >= 0; lig--)
                {
                    if (tab[lig, i] == 0)
                    {
                        if (j1aletrait)
                            tab[lig, i] = 1;
                        else
                            tab[lig, i] = 2;

                        if (lig == 0)
                        {
                            NbCoups--; //mise a jour de NbCoups
                        }

                        j1aletrait = !j1aletrait;
                        break;
                    }
                    
                }

                // mise a jour de Eval
                Evaluation(1);
                Evaluation(2);
                Verif_Match_Null();
            }
        }

        //Methode pour faire la mise a jour de Eval
        public void Evaluation(int j)
        {
            int aux = 1;
            for (int col = 0; col < 7; col++)
            {
                for (int lig = 5; lig >=0; lig--)
                {
                    if (tab[lig, col] == j)
                    {
                        //Verification de 4 verticales
                        for (int tmp = 1; tmp <= 3; tmp++)
                        {
                            if (lig - tmp >= 0)
                            {
                                if (tab[lig - tmp, col] == j)
                                    aux++;
                            }
                        }
                        if (aux == 4)
                        {
                            Eval = (!j1aletrait)? Resultat.j1gagne :Resultat.j0gagne; 
                            NbCoups = 0;
                        }
                        else
                        {
                            aux = 1;
                        }

                        //Verification de 4 horizontales
                        for (int tmp = 1; tmp <= 3; tmp++)
                        {
                            if (col + tmp <= 6)
                            {
                                if (tab[lig, col + tmp] == j)
                                    aux++;
                            }
                        }
                        if (aux == 4)
                        {
                            Eval = (!j1aletrait) ? Resultat.j1gagne : Resultat.j0gagne;
                            NbCoups = 0;
                        }
                        else
                        {
                            aux = 1;
                        }

                        //Verification de 4 diagonales de la gauche vers la droite
                        for (int tmp = 1; tmp <= 3; tmp++)
                        {
                            if (lig - tmp >= 0 && col + tmp <= 6)
                            {
                                if (tab[lig - tmp, col + tmp] == j)
                                    aux++;
                            }
                        }
                        if (aux == 4)
                        {
                            Eval = (!j1aletrait) ? Resultat.j1gagne : Resultat.j0gagne;
                            NbCoups = 0;
                        }
                        else
                        {
                            aux = 1;
                        }

                        //Verification de 4 diagonales de la droite vers gauche
                        for (int tmp = 1; tmp <= 3; tmp++)
                        {
                            if (lig + tmp <= 5 && col + tmp <= 6 )
                            {
                                if (tab[lig + tmp, col + tmp] == j)
                                    aux++;
                            }
                        }
                        if (aux == 4)
                        {
                            Eval = (!j1aletrait) ? Resultat.j1gagne : Resultat.j0gagne;
                            NbCoups = 0;
                        }
                        else
                        {
                            aux = 1;
                        }
                    }
                }
            }
        }

        //Methode pour verifier si la resultat est un match nul.
        public void Verif_Match_Null()
        {
            int a = 0;
            for(int col = 0; col <= 6; col++)
            {
                if (tab[0, col] != 0)
                    a++;
                if (a == 7)
                {
                    Eval = Resultat.partieNulle;
                    NbCoups = 0;
                }
            }
        }

        public override Position Clone()
        {
            PositionP4 p = new PositionP4(j1aletrait);
            p.NbCoups = NbCoups;
            p.Eval = Eval;
            return p;
        }

        public override void Affiche()
        {
            if (Eval == Resultat.indetermine)
            {
                if (j1aletrait)
                    Console.WriteLine("C'est le tour de Joueur1!");
                else
                    Console.WriteLine("C'est le tour de Joeur0!");

                for (int i = 0; i < 7; i++)
                {
                    Console.Write(i + "\t");
                }
                Console.WriteLine("");
                Console.WriteLine("________________________________________________________");

                for (int i = 0; i < 6; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        if (tab[i, j] == 1)
                            Console.Write("X \t");
                        if (tab[i, j] == 2)
                            Console.Write("O \t");
                        if (tab[i, j] == 0)
                            Console.Write("  \t");
                    }
                    Console.WriteLine("| " + i);
                }
            }
            else if (Eval == Resultat.partieNulle)
                Console.WriteLine("La partie est terminée et personne a gagné!");
            else
                Console.WriteLine("La partie est terminée!");
        }
    }

    class JoueurHumainPuissance4 : Joueur
    {
        Position p; 

        public JoueurHumainPuissance4(Position p)
        {
            this.p = p;
        }

        public override int Jouer(Position p)
        {
            int nb;

            Console.WriteLine("Quelle colonne voulez vous remplir?");
            nb = int.Parse(Console.ReadLine());
            Console.WriteLine("");

            while (nb <0 ||nb > 6)
            {
                Console.WriteLine("Vous devez rempli entre la colonne 1 et 6!");
                Console.WriteLine("Quelle colonne voulez vous remplir?");
                nb = int.Parse(Console.ReadLine());
                Console.WriteLine("");
            }
            return nb;
        }
    }

    public class JMCTSp : Joueur
    {
        public static Random[] gen;
        static Stopwatch sw = new Stopwatch();

        float a, b;
        int temps;
        JMCTS[] jmctp;
        Task<int>[] t;

        Noeud racine;

        public JMCTSp(float a, float b, int temps, int N)
        {
            this.a = 2 * a;
            this.b = 2 * b;
            this.temps = temps;
            jmctp = new JMCTS[N];
            gen = new Random[N];

            for(int i = 0; i<N; i++)
            {
                jmctp[i] = new JMCTS(a, b, temps);
                gen[i] = new Random();
            }
            t = new Task<int>[jmctp.Length];
        }

        public override string ToString()
        {
            return string.Format("JMCTSp[{0} - {1} - temps={2}]", a / 2, b / 2, temps);
        }

        int JeuHasard(Position p, int i)
        {
            Position q = p.Clone();
            int re = 1;
            
            while (q.NbCoups > 0)
            {
                q.EffectuerCoup(gen[i].Next(0, q.NbCoups));
            }
            if (q.Eval == Resultat.j1gagne) { re = 2; }
            if (q.Eval == Resultat.j0gagne) { re = 0; }
            return re;
        }


        public override int Jouer(Position p)
        {
            sw.Restart();
            Func<int, int, float> phi = (W, C) => (a + W) / (b + C);

            racine = new Noeud(null, p);
            int iter = 0;
            while (sw.ElapsedMilliseconds < temps)
            {
                Noeud no = racine;

                do // Sélection
                {
                    no.CalculMeilleurFils(phi);
                    no = no.MeilleurFils();

                } while (no.cross > 0 && no.fils.Length > 0);

                // Simulation
                for(int i = 0; i < jmctp.Length; i++)
                {
                    int j = i;
                    t[i] = Task.Factory.StartNew(() => JeuHasard(no.p, j));
                }
                Task.WaitAll(t);

                while (no != null) // Rétropropagation
                {
                    no.cross += 2;
                    for(int i = 0; i<t.Length; i++)
                    {
                        no.win += t[i].Result;
                    }
                    no = no.pere;
                }
                iter++;
            }
            racine.CalculMeilleurFils(phi);
            Console.WriteLine("{0} itérations", iter);
            Console.WriteLine(racine);
            return racine.indiceMeilleurFils;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            /*PositionA p_initiale = new PositionA(true,21);
            JouerHumainA j1 = new JouerHumainA(p_initiale);
            JouerHumainA j2 = new JouerHumainA(p_initiale);
            JMCTS j3 = new JMCTS(2, 2, 100);
            JMCTS j4 = new JMCTS(3, 3, 100);

            //Partie partie = new Partie(j1, j2, p_initiale);
            //partie.Commencer();
            Partie partie2 = new Partie(j4, j3, p_initiale);
            partie2.Commencer();*/

            /*int[,] tab = new int[6, 7];
            PositionP4 p_initiale = new PositionP4(true);
            JoueurHumainPuissance4 J1 = new JoueurHumainPuissance4(p_initiale);
            JoueurHumainPuissance4 J2 = new JoueurHumainPuissance4(p_initiale);
            JMCTS J3 = new JMCTS(1, 1, 100);
            JMCTS J4 = new JMCTS(2, 2, 100);
            JMCTSp J5 = new JMCTSp(2, 2, 100, 4);
            Partie partie = new Partie(J1, J5, p_initiale);
            partie.Commencer();*/

            //Le championale

            int NB = 50;
            int[] tab_score = new int[NB];
            for (int a = 1; a <= NB; a++)
            {
                int aux = 0;
                for(int i = 1; i <= NB; i++)
                {
                    int[,] tab_c = new int[6, 7];
                    PositionP4 p_initiale_c = new PositionP4(true);
                    JMCTS J6 = new JMCTS(a, a, 100);
                    JMCTS J7 = new JMCTS(i, i, 100);
                    Partie partie_c = new Partie(J6, J7, p_initiale_c);
                    
                    partie_c.Commencer();

                    if (partie_c.r == Resultat.j1gagne)
                        aux++; 
                }
                tab_score[a - 1] = aux;
                
            }
            for(int i = 0; i<=NB-1; i++)
            {
                Console.WriteLine("Le score de a = {0} est {1} / {2}", i + 1, tab_score[i], NB);
            }

            /*//la performance entre JMCTS et JMCTSp
            int score_JMCTS = 0, score_JMCTSp = 0;
            for (int i = 0; i< 30; i++)
            {
                int[,] tab_p = new int[6, 7];
                PositionP4 p_initiale_p = new PositionP4(true);
                JMCTS J8 = new JMCTS(14, 14, 100);
                JMCTSp J9 = new JMCTSp(14, 14, 100, 4);
                Partie partie_p = new Partie(J8, J9, p_initiale_p);

                partie_p.Commencer();
                
                if (partie_p.r == Resultat.j1gagne)
                    score_JMCTS++;
                if (partie_p.r == Resultat.j0gagne)
                    score_JMCTSp++;
            }
            Console.WriteLine("Le score de JMCTS est: " + score_JMCTS);
            Console.WriteLine("Le score de JMCTSp est: " + score_JMCTSp);*/
        }
    }
}
