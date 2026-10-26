using System.Linq.Expressions;

namespace Ticketing.Domain.Specifications;

public abstract class Specification<T>
{
    public List<Expression<Func<T, bool>>> Criteria { get; } = [];
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
    public int? Skip { get; protected set; }
    public int? Take { get; protected set; }
}
