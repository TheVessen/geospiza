using System;
using GeospizaManager.GeospizaCordinator;

namespace GeospizaManager.GeospizaCordinator
{
    public class EvolutionarySolverCoordinator
    {
        private static EvolutionarySolverCoordinator _instance;
        private static readonly object Padlock = new object();
        private int _numberOfSolvers;
        private string _ghFilePath;
        private HttpServer _httpServer;

        private EvolutionarySolverCoordinator(string ghFilePath, int numberOfSolvers)
        {
            _ghFilePath = ghFilePath;
            _numberOfSolvers = numberOfSolvers;
            _httpServer = HttpServer.Instance();
        }

        public static EvolutionarySolverCoordinator Instance(string ghFilePath, int numberOfSolvers)
        {
            lock (Padlock)
            {
                if (_instance == null)
                {
                    _instance = new EvolutionarySolverCoordinator(ghFilePath, numberOfSolvers);
                }
                return _instance;
            }
        }

        public void StartHttpServer()
        {
            _httpServer.Start("http://localhost:8080/");
        }

        public void StopHttpServer()
        {
            _httpServer.Stop();
        }

        public static bool IsInstantiated()
        {
            return _instance != null;
        }
    }
}