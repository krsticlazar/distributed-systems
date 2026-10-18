import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

public class LibraryImpl extends UnicastRemoteObject implements Library {
    private final Map<Integer, Book> books = new LinkedHashMap<>();

    public LibraryImpl() throws RemoteException {
        super();
        books.put(1, new BookImpl(1, "Na Drini cuprija", "Ivo Andric"));
        books.put(2, new BookImpl(2, "Dervis i smrt", "Mesa Selimovic"));
    }

    public synchronized Book getBook(int id) throws RemoteException {
        return books.get(id);
    }

    public synchronized List<BookInfo> getAllBooks() throws RemoteException {
        List<BookInfo> rezultat = new ArrayList<>();

        for (Book book : books.values()) {
            rezultat.add(book.getInfo());
        }

        return rezultat;
    }
}
