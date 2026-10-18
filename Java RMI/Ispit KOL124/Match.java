import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Match extends Remote {
    void addHomeGoal() throws RemoteException;
    void addAwayGoal() throws RemoteException;
    Stadium getStadium() throws RemoteException;
    String getResult() throws RemoteException;
    void subscribe(Callback callback) throws RemoteException;
}
