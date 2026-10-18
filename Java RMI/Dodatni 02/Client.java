import java.rmi.Naming;

public class Client {
    public static void main(String[] args) {
        try {
            String host = args.length > 0 ? args[0] : "localhost";
            int bookId = args.length > 1 ? Integer.parseInt(args[1]) : 1;

            Library library = (Library) Naming.lookup(
                    "rmi://" + host + ":2099/Library");

            System.out.println("Sve knjige:");
            for (BookInfo info : library.getAllBooks()) {
                System.out.println(info);
            }

            Book book = library.getBook(bookId);
            boolean borrowed = book.borrow();
            System.out.println("Iznajmljivanje uspesno: " + borrowed);
            System.out.println("Novo stanje: " + book.getInfo());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
