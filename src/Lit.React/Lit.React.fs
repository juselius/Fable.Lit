namespace Lit

open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Fable.React
open Browser.Types
open Lit

/// <summary>
/// Directive that allows a react component to be rendered inside a Lit template.
/// </summary>
[<AttachMembers>]
type ReactDirective() =
    inherit Types.AsyncDirective()

    let mutable _domEl = Unchecked.defaultof<Element>

    member _.className = ""
    member _.renderFn = Unchecked.defaultof<obj -> ReactElement>

    member this.render(props: obj) =
        Lit.html $"""<div class={this.className} {Lit.refCallback(function
            | Some el when this.isConnected ->
                _domEl <- el
                let reactEl = this.renderFn props
                let root = ReactDOM.createRoot el
                root.render(reactEl)
            | _ -> ()
        )}></div>"""

    member _.disconnected() =
        if not(isNull _domEl) then
            ReactDom.unmountComponentAtNode(_domEl) |> ignore

type React =
    /// <summary>
    /// Renders a React element into a Lit template
    /// </summary>
    /// <param name="reactComponent">The function that will be called to render the component.</param>
    /// <param name="className">The class name to apply to the rendered element.</param>
    /// <returns>A <see cref="Lit.TemplateResult">TemplateResult</see></returns>
    static member toLit (reactComponent: 'Props -> ReactElement, ?className: string): 'Props -> TemplateResult =
        emitJsExpr (jsConstructor<ReactDirective>, reactComponent, defaultArg className "")
            "class extends $0 { renderFn = $1; className = $2 }"
        |> LitBindings.directive :?> _

    /// <summary>
    /// Renders a Lit template into a React element
    /// </summary>
    /// <param name="template">A Lit template result.</param>
    /// <param name="tag">the name of the tag that will wrap the Lit template result .</param>
    /// <param name="className">a class name for the wrapper element.</param>
    /// <returns>A ReactElement</returns>
    static member inline ofLit (template: TemplateResult, ?tag: string, ?className: string) =
        let tag = defaultArg tag "div"
        let container = Hooks.useRef Unchecked.defaultof<HTMLElement option>
        Hooks.useEffect((fun () ->
            match container.current with
            | None -> ()
            | Some el -> template |> Lit.render el
        ))
        Interop.createElement tag [
            prop.classes [ (defaultArg className "") ]
            prop.ref container
        ]

    /// Renders a Lit HTML template as a ReactElement.
    /// Must be used at the root of a React functional component (like a hook).
    static member inline lit_html (s: FormattableString) =
        React.ofLit(Lit.html s)

    /// Renders a Lit SVG template as a ReactElement.
    /// svg is required for nested templates within an svg element.
    /// Must be used at the root of a React functional component (like a hook).
    static member inline lit_svg (s: FormattableString) =
        React.ofLit(Lit.html s)