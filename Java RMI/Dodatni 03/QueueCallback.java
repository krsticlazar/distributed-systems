import java.rmi.Remote;
import java.rmi.RemoteException;

public interface QueueCallback extends Remote {
    void ticketCalled(int number) throws RemoteException;
}
