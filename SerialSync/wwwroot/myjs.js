window.tools = {
    getElementHeight: (elementId) => {
        const element = document.getElementById(elementId);
        return element ? element.clientHeight : 0;
    },
    addResizeListener:(element, dotNetHelper) =>{
        const resizeObserver = new ResizeObserver(entries => {
            dotNetHelper.invokeMethodAsync('OnResize');
        });
        resizeObserver.observe(element);
    }
};