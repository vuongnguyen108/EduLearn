$(function () {
    const filterUrl = "/Course/Filter";

    let currentPage = 1;
    let currentRequest = null;

    // Encode nội dung để tránh chèn HTML không an toàn
    function escapeHtml(value) {
        return String(value ?? "").replace(/[&<>"']/g, function (char) {
            return {
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                '"': "&quot;",
                "'": "&#39;"
            }[char];
        });
    }

    function formatPrice(price) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
            maximumFractionDigits: 0
        }).format(price ?? 0);
    }
    function getResponsivePageSize() {
        const width = window.innerWidth;

        if (width < 576) {
            return 4; // Mobile
        }

        if (width < 768) {
            return 6; // Tablet nhỏ
        }

        return 8; // Tablet lớn và PC
    }
    function getFilters() {
        return {
            categoryId: $("#categoryFilter").val() || "",
            minPrice: $("#minPrice").val() || "",
            maxPrice: $("#maxPrice").val() || "",
            level: $("#levelFilter").val() || "",
            searchTerm: $("#headerSearchInput").val()?.trim() || "",
            page: currentPage,
            pageSize: getResponsivePageSize()
        };
    }


    function renderCourses(courses) {

        if (!courses || courses.length === 0) {
            $("#courseResults").html(`
                <div class="col-12">
                    <div class="d-flex justify-content-center align-items-center text-center"
                         style="min-height: calc(100vh - 350px); width: 100%;">
                        <div>
                            <i class="bi bi-search fs-1 text-secondary"></i>

                            <h5 class="mt-3">No courses found</h5>

                            <p class="text-muted">
                                There are no courses matching your filters.
                                Try selecting another category.
                            </p>

                            <button type="button"
                                    class="btn btn-primary"
                                    id="emptyResetFilters">
                                Clear filters
                            </button>
                        </div>
                    </div>
                </div>
            `);
            return;
        }

        let html = "";

        courses.forEach(function (item) {
            const courseId = escapeHtml(item.courseId);
            const title = escapeHtml(item.courseTitle);
            const description = escapeHtml(item.description);
            const level = escapeHtml(item.level);
            const thumbnail = escapeHtml(item.thumbnailUrl);

            let priceHtml = "";

            if (item.discountPrice != null) {
                priceHtml = `
                <span class="fw-bold text-dark">
                    ${formatPrice(item.discountPrice)}
                </span>
                <span class="text-muted text-decoration-line-through small ms-1">
                    ${formatPrice(item.price)}
                </span>
            `;
            } else {
                priceHtml = `
                <span class="fw-bold text-dark">
                    ${formatPrice(item.price)}
                </span>
            `;
            }

            html += `
            <div class="col-12 col-sm-6 col-lg-4 col-xl-3">
                <div class="card course-card h-100 border-0 shadow-sm">

                    <a href="/Course/Details/${courseId}"
                       class="text-decoration-none text-reset">

                        <img src="${thumbnail}"
                             class="card-img-top course-card-thumb"
                             alt="${title}"
                             loading="lazy" />
                    </a>

                    <div class="card-body d-flex flex-column">

                        <span class="badge text-secondary align-self-start mb-2"
                              style="background-color: #dadada;">
                            ${level}
                        </span>

                        <h3 class="course-card-title fw-semibold">
                            ${title}
                        </h3>

                        <p class="course-card-description text-muted small mb-3">
                            ${description}
                        </p>

                        <div class="mt-auto pt-2">
                            ${priceHtml}
                        </div>

                    </div>
                </div>
            </div>
        `;
        });

        $("#courseResults").html(html);
    }


    function renderPagination(data) {
        const totalPages = data.totalPages || 0;
        const page = data.currentPage || 1;

        if (totalPages <= 1) {
            $("#pagination").empty();
            return;
        }

        let html = "";

        html += `
            <li class="page-item ${page <= 1 ? "disabled" : ""} mx-1">
                <button class="page-link"
                        data-page="${page - 1}"
                        aria-label="Previous page"
                        ${page <= 1 ? "disabled" : ""}>
                    <i class="bi bi-chevron-left"></i>
                </button>
            </li>
        `;

        for (let i = 1; i <= totalPages; i++) {
            html += `
                <li class="page-item ${i === page ? "active" : ""} mx-1">
                    <button class="page-link" data-page="${i}">
                        ${i}
                    </button>
                </li>
            `;
        }

        html += `
            <li class="page-item ${page >= totalPages ? "disabled" : ""} mx-1">
                <button class="page-link"
                        data-page="${page + 1}"
                        aria-label="Next page"
                        ${page >= totalPages ? "disabled" : ""}>
                    <i class="bi bi-chevron-right"></i>
                </button>
            </li>
        `;

        $("#pagination").html(html);
    }

    function loadCourses() {
        // Hủy request trước nếu người dùng thay đổi bộ lọc liên tục
        if (currentRequest) {
            currentRequest.abort();
        }

        $("#courseLoading").removeClass("d-none");
        $("#courseResults").addClass("opacity-50");

        currentRequest = $.ajax({
            url: filterUrl,
            type: "GET",
            data: getFilters(),
            dataType: "json",

            success: function (data) {
                console.log(data.courses);
                renderCourses(data.courses);

                $("#resultCount").text(data.totalResults);
                renderPagination(data);
            },

            error: function (xhr, status, error) {
                if (status === "abort") return;

                console.error("Course filter error:", error);
                console.error(xhr.responseText);

                $("#courseResults").html(`
                    <div class="col-12">
                        <p class="text-danger text-center py-4">
                            Unable to load courses. Please try again.
                        </p>
                    </div>
                `);
            },

            complete: function () {
                $("#courseLoading").addClass("d-none");
                $("#courseResults").removeClass("opacity-50");
                currentRequest = null;
            }
        });
    }

    // Bộ lọc thay đổi: quay về trang đầu
    $("#categoryFilter, #levelFilter, #sortBy").on("change", function () {
        currentPage = 1;
        loadCourses();
    });

    $("#minPrice, #maxPrice").on("change", function () {
        const min = $("#minPrice").val();
        const max = $("#maxPrice").val();

        if (min !== "" && Number(min) < 0) {
            alert("Minimum price cannot be negative.");
            return;
        }

        if (max !== "" && Number(max) < 0) {
            alert("Maximum price cannot be negative.");
            return;
        }

        if (min !== "" && max !== "" && Number(min) > Number(max)) {
            alert("Minimum price cannot exceed maximum price.");
            return;
        }

        currentPage = 1;
        loadCourses();
    });

    // Chuyển trang
    $("#pagination").on("click", "[data-page]", function () {
        const page = Number($(this).attr("data-page"));

        if (page < 1 || !Number.isInteger(page)) return;

        currentPage = page;
        loadCourses();

        window.scrollTo({
            top: $(".course-list-page").offset().top - 20,
            behavior: "smooth"
        });
    });

    // Xóa bộ lọc
    function resetFilters() {
        $("#categoryFilter").val("");
        $("#minPrice").val("");
        $("#maxPrice").val("");
        $("#levelFilter").val("");
        $("#sortBy").val("popular");

        currentPage = 1;
        loadCourses();
    }

    $("#resetFilters").on("click", resetFilters);

    $("#courseResults").on("click", "#emptyResetFilters", resetFilters);
    // Tìm kiếm khóa học
    $("#headerSearchButton").on("click", function () {
        currentPage = 1;
        loadCourses();
    });

    // Nhấn Enter để tìm kiếm
    $("#headerSearchInput").on("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            currentPage = 1;
            loadCourses();
        }
    });

    $("#headerSearchForm").on("submit", function (e) {
        if ($(".course-list-page").length > 0) {
            e.preventDefault();
            currentPage = 1;
            loadCourses();
        }
    });

    // Tải danh sách lần đầu
    loadCourses();
});
