import java.io.Serializable;

public class BookInfo implements Serializable {
    private static final long serialVersionUID = 1L;

    private final int id;
    private final String title;
    private final String author;
    private final boolean available;

    public BookInfo(int id, String title, String author, boolean available) {
        this.id = id;
        this.title = title;
        this.author = author;
        this.available = available;
    }

    public int getId() {
        return id;
    }

    public String getTitle() {
        return title;
    }

    public String getAuthor() {
        return author;
    }

    public boolean isAvailable() {
        return available;
    }

    public String toString() {
        return id + ": " + title + " - " + author + ", dostupna=" + available;
    }
}
