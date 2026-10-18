import java.rmi.Remote;
import java.rmi.RemoteException;

public interface QueueService extends Remote {
    Ticket takeTicket(String clientName, QueueCallback callback) throws RemoteException;
    String getQueue() throws RemoteException;
    void callNext() throws RemoteException;
}
