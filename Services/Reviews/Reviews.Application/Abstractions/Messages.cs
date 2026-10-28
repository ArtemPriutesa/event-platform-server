using MediatR;

namespace Reviews.Application.Abstractions;

public interface ICommand : IRequest;
public interface ICommand<out TResponse> : IRequest<TResponse>;
public interface IQuery<out TResponse> : IRequest<TResponse>;
