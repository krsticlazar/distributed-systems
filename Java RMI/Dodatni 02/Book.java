import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Book extends Remote {
    boolean borrow() throws RemoteException;
    void returnBook() throws RemoteException;
    BookInfo getInfo() throws RemoteException;
}
