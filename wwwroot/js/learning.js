$(function () {
    const page = document.querySelector(".learning-page");

    if (!page) return;

    const courseId = page.dataset.courseId;
    const lessonId = page.dataset.lessonId;
    const totalLessons = Number(page.dataset.totalLessons || 0);

    const storageKey = `learning_completed_${courseId}`;

    function getCompletedLessons() {
        try {
            return JSON.parse(sessionStorage.getItem(storageKey) || "[]");
        } catch {
            return [];
        }
    }

    function saveCompletedLessons(lessons) {
        sessionStorage.setItem(storageKey, JSON.stringify(lessons));
    }

    function updateProgress() {
        const completed = getCompletedLessons();
        const completedCount = completed.length;

        const percent = totalLessons > 0
            ? Math.round(completedCount / totalLessons * 100)
            : 0;

        $("#learningProgressBar")
            .css("width", `${percent}%`)
            .attr("aria-valuenow", percent);

        $("#learningProgressText")
            .text(`${percent}% complete`);

        $(".learning-lesson").each(function () {
            const id = Number(this.dataset.lessonId);

            $(this).toggleClass(
                "completed",
                completed.includes(id)
            );
        });

        const currentId = Number(lessonId);
        const isCompleted = completed.includes(currentId);

        $("#markCompleteButton")
            .prop("disabled", isCompleted)
            .html(
                isCompleted
                    ? '<i class="bi bi-check-circle-fill me-1"></i> Completed'
                    : '<i class="bi bi-check-circle me-1"></i> Mark as Complete'
            );
    }

    $("#markCompleteButton").on("click", function () {
        const currentId = Number(lessonId);

        if (!currentId) return;

        const completed = getCompletedLessons();

        if (!completed.includes(currentId)) {
            completed.push(currentId);
            saveCompletedLessons(completed);
        }

        updateProgress();
    });

    updateProgress();
});
