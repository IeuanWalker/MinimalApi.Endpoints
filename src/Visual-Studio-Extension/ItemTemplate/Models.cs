namespace $fileinputname$;

sealed class Request
{


    internal sealed class Validator : Validator<Request>
    {
        public Validator()
        {

        }
    }
}

sealed class Response
{
    public string Message => "This endpoint hasn't been implemented yet!";
}
