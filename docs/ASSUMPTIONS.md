# Assumptions

The assignment leaves several business rules unspecified. These are explicit assumptions rather than hidden behaviour.

- **Rental day:** a started rental day counts as one day; elapsed rental time is rounded up to whole days with a minimum of one day after pickup.
- **Kilometers:** `returnOdometer - pickupOdometer`.
- **Invalid odometer:** return odometer below pickup odometer is rejected.
- **Return time:** must not be before pickup time.
- **Double return:** a returned rental cannot be returned again.
- **Price rounding:** no rounding is applied because the specification does not define a rounding policy.
- **Pricing:** base daily and kilometer prices are supplied to the return operation. The specification does not define where these values are stored or precisely when they become fixed for a rental.
- **Currency:** not modeled because currency is not specified.
- **Persistence:** the core does not depend on a database. The reference API uses an in-memory adapter; customer examples demonstrate independent persistence choices.
