import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class BookImpl extends UnicastRemoteObject implements Book {
    private final int id;
    private final String title;
    private final String author;
    private boolean available = true;

    public BookImpl(int id, String title, String author) throws RemoteException {
        super();
        this.id = id;
        this.title = title;
        this.author = author;
    }

    public synchronized boolean borrow() throws RemoteException {
        if (!available) {
            return false;
        }

        available = false;
        return true;
    }

    public synchronized void returnBook() throws RemoteException {
        available = true;
    }

    public synchronized BookInfo getInfo() throws RemoteException {
        return new BookInfo(id, title, author, available);
    }
}
