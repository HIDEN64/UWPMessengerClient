namespace UWPMessengerClient.MSNP
{
    //the list numbers of messenger https://wiki.nina.bz/wiki/Protocols/MSNP/MSNP8/Getting_Details#List_numbers
    public enum ListNumbers
    {
        Forward = 1,
        Allow = 2,
        Block = 4,
        Reverse = 8,
        Pending = 16
    }
}