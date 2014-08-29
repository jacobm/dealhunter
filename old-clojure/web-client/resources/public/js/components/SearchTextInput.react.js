/** @jsx React.DOM */ 

var React = require('react');
var ReactPropTypes = React.PropTypes;

var ENTER_KEY_CODE = 13;

var SearchTextInput = React.createClass({
    propTypes: {
	className: ReactPropTypes.string,
	onSubmit: ReactPropTypes.func.isRequired,
	value: ReactPropTypes.string
    },

    getInitialState: function() {
	return {value : this.props.value || ''};
    },

    render: function() {
	    return (
      <input
        className={this.props.className}
        onChange={this._onChange}
        onKeyDown={this._onKeyDown}
        value={this.state.value}
	placeholder="Search"
        autoFocus={true}
      />
    )},

    _save: function() {
	this.props.onSubmit(this.state.value);
	this.setState({value: ''});
    },
    
    _onChange: function(/*object*/ event) {
	this.setState({
	    value: event.target.value
	});
    },
    
    _onKeyDown: function(event) {
	if (event.keyCode === ENTER_KEY_CODE) {
	    this._save();
	}
    }
});

module.exports = SearchTextInput;
