import java.rmi.Remote;
import java.rmi.RemoteException;

public interface FootballScore extends Remote {
    String getAllResults() throws RemoteException;
    Match getMatch(int id) throws RemoteException;
}
