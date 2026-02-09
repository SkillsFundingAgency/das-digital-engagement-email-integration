# TimerTrigger - C#

This sample demonstrates how to use a `TimerTrigger` in a .NET 8 Azure Function to execute code on a schedule.

## How it works

A `TimerTrigger` allows you to run your function based on a schedule defined by a [cron expression]. Cron expressions are strings with 6 fields representing: seconds, minutes, hours, day of month, month, and day of week.

For example, the cron expression `0 */5 * * * *` means:
- At second 0,
- Every 5 minutes,
- Every hour,
- Every day of the month,
- Every month,
- Every day of the week.

This will trigger the function every 5 minutes.

## Example Function

- The schedule is configured via the `EmailIntegrationSchedule` setting in `local.settings.json`.
- The function logs the execution time each time it runs.

## Configuration

To run locally, add the following to your `local.settings.json`:

- `EmailIntegrationSchedule`: Cron expression for the timer. Example: `0/5 * * * * *` triggers every 5 seconds. For daily at 10pm, use `0 0 22 * * *`.

## Learn more

- [Azure Functions TimerTrigger documentation](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer)
