# Compiled Validators

CompiledValidators is a blazingly fast validator that takes advantage 
of static analysis at runtime to inspect your objects, generate a compiled
LINQ expression, and validate your objects.

What this means is that **CompiledValidators is 97 times faster than DataAnnotations**
when validating a simple type with a single field and a range attribute (see Performance.cs).
The performance gains only start here. As types get increasingly complex, the cost of 
reflection continues to accumulate, while the cost of analysis for CompiledValidators is
borne only once.

CompiledValidators is also smart enough to dig into your objects and keep validating the
object graph all the way to the leaves. It can also iterate your IEnumerable<T> members
and validate all of its items.

## Getting Started

You can get the latest stable release from the Downloads section on the project site at GitHub, 
or you can use Nuget:

    Install-Package CompiledValidators.DataAnnotations

Or, if you want to use the core assembly directly:

    Install-Package CompiledValidators

## Integration With Your Solution

The core of CompildedValidators does not take a dependency on absolutely anything,
so you can plug it into your current validation framework, even if it's completely homegrown.
All you need to do is implement a few interfaces that tell CompiledValidators how to hook
into your validation system.

Out of the box, CompiledValidators comes with total feature parity with DataAnnotations 
(CompiledValidators.DataAnnotations.dll), which, by the way, was implemented with four 
classes and a total of 54 SLoC. It's really that easy.

CompiledValidators plays nice with your dependency injection container, too. Just register
`Validator` as `IValidator` and its injected components (`IRecursionPolicy`, `IValidatorProvider`,
`IEnumerable<IValidationExpressionConverter>`) and you're off to the races.

You can use CompiledValidators without a dependency injection container, as well:

    using CompiledValidators;
    using CompiledValidators.DataAnnotations;

    Validator.Default = new Validator(
        isThreadSafe: true,
        new UserAssemblyRecursionPolicy(),
        new DataAnnotationsValidatorProvider(),
        new RangeValidationExpressionConverter(),
        new ValidatableObjectValidationExpressionConverter(),
        new DefaultValidationExpressionConverter());

    Validator.Default.IsValid(myObj);

## Concepts

### Validation Optimism

Hopefully, most of your objects are valid. The trouble is finding the 10 or so percent of objects
that aren't. To that end, CompiledValidators gives you three methods on `Validator` to suit your needs:

#### IsValid

Fast and no-nonsense, validates until it finds an error to return false. Creates zero objects
post-analysis.

#### ValidateToFirstError

For times when a little bit of data would go a long way. This finds the first invalid object and returns
it, also creating zero objects post-analysis. Some small overhead involved in obtaining the member name.

#### Validate

When you need everything, you can get it. If you specify the `isOptimistic` flag to be `true`
(it is by default), CompiledValidators will do a first pass to see if your object is valid.
If it is invalid or `isOptimistic` is set to `false`, a list must be allocated to hold the errors
and no further objects are created.

### Recursion Policies

A recursion policy tells CompiledValidators when to *not* dig into a member (CompiledValidators chooses when
a member is a good recursion candidate). CompiledValidators comes with `UserAssemblyRecursionPolicy` which
prevents any type in a .NET Framework assembly from being recursed into.

### Validator Providers

A validator provider implements `IValidatorProvider` and is responsible for looking at a type or a member
and identifying all potential validators. CompiledValidators comes with `AttributeValidatorProvider` which
identifies all attributes as validators.

ValidationProviders also tell CompildValidators what, if any, error message should be returned. 

* If a `ValidatorInfo` is returned, CompiledValidators will choose a default unspecified error message.
* If an `ErrorMessageValidatorInfo` is returned, its error message is evaluated per validator and cached.
* If a `MemberErrorValidationInfo` is returned, its error factory is called for every invalid object found
    and cannot thus be cached.

### Validation Expression Converters

This is where the magic happens. A validation expression converter implements `IValidationExpressionConverter`
and can choose members it would like to validate. The first converter (evaluated in given order) that can 
convert a validator for a member is assigned as that validator's converter. It must then produce an expression
that would return a boolean expressing validity.

## To-Do

* Unity configuration example
* AutoFac configuration example
* StructureMap configuration example
