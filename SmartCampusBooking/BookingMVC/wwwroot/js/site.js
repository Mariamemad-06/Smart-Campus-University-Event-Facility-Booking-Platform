// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Notification Bell
document.addEventListener("DOMContentLoaded", function () {

    const badge = document.getElementById("notificationBadge");

    if (!badge)
        return;

    const countUrl = badge.dataset.countUrl;

    let previousCount = null;

    async function updateNotificationCount() {
        try {
            const response = await fetch(countUrl, {
                method: "GET",
                credentials: "same-origin"
            });

            if (!response.ok)
                return;

            const data = await response.json();
            const count = data.count ?? 0;

            // Update badge number
            if (count > 0) {
                badge.textContent = count > 99 ? "99+" : count;
                badge.classList.remove("d-none");
            }
            else {
                badge.textContent = "";
                badge.classList.add("d-none");
            }

            // Ring the bell when the unread count increases
            if (previousCount !== null && count > previousCount) {
                const bell = document.querySelector(".notification-bell-icon");

                if (bell) {
                    bell.classList.remove("ringing");

                    // Restart animation
                    void bell.offsetWidth;

                    bell.classList.add("ringing");
                }
            }

            previousCount = count;
        }
        catch (error) {
            console.error("Failed to retrieve unread notifications:", error);
        }
    }

    // Fetch immediately when page loads
    updateNotificationCount();

    // Check again every 15 seconds
    setInterval(updateNotificationCount, 15000);
});