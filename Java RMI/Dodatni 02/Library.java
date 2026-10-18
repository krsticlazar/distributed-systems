import java.rmi.Remote;
import java.rmi.RemoteException;
import java.util.List;

public interface Library extends Remote {
    Book getBook(int id) throws RemoteException;
    List<BookInfo> getAllBooks() throws RemoteException;
}
